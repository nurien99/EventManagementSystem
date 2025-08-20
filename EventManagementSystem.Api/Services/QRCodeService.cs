using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using System.Text.Json;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using QRCoder;

namespace EventManagementSystem.Api.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(
            IDataProtectionProvider dataProtectionProvider,
            ApplicationDbContext context,
            ILogger<QRCodeService> logger)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _context = context;
            _logger = logger;
        }

        public async Task<string> GenerateTicketQRCodeBase64Async(string ticketData)
        {
            try
            {
                var qrBytes = await GenerateTicketQRCodeAsync(ticketData);
                return Convert.ToBase64String(qrBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Base64 QR code for ticket data: {TicketData}", ticketData);
                throw;
            }
        }

        public async Task<string> GenerateTicketQRCodeBase64Async(int registrationId, int ticketTypeId, string userEmail)
        {
            try
            {
                var secureTicketData = await GenerateSecureTicketDataAsync(registrationId, ticketTypeId, userEmail);
                var qrBytes = await GenerateTicketQRCodeAsync(secureTicketData);
                return Convert.ToBase64String(qrBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Base64 QR code for registration {RegistrationId}", registrationId);
                throw;
            }
        }

        public async Task<byte[]> GenerateTicketQRCodeAsync(string ticketData)
        {
            try
            {
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Height = 200,
                        Width = 200,
                        Margin = 10
                    }
                };

                using var bitmap = writer.Write(ticketData);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);

                return data.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for ticket data: {TicketData}", ticketData);
                throw;
            }
        }

        public async Task<byte[]> GenerateEventQRCodeAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    throw new ArgumentException($"Event with ID {eventId} not found");
                }

                var eventUrl = $"https://localhost:7203/events/{eventEntity.UrlSlug}";
                return await GenerateTicketQRCodeAsync(eventUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for event ID: {EventId}", eventId);
                throw;
            }
        }

        public async Task<string> GenerateSecureTicketDataAsync(int registrationId, int ticketTypeId, string userEmail)
        {
            try
            {
                var protector = _dataProtectionProvider.CreateProtector("TicketQRCode");

                var ticketPayload = new TicketQRPayload
                {
                    RegistrationId = registrationId,
                    TicketTypeId = ticketTypeId,
                    UserEmail = userEmail,
                    IssuedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30), // Tickets valid for 30 days
                    Signature = GenerateSignature(registrationId, ticketTypeId, userEmail)
                };

                var jsonPayload = JsonSerializer.Serialize(ticketPayload);
                var encryptedPayload = protector.Protect(jsonPayload);

                _logger.LogInformation("Generated secure ticket data for registration {RegistrationId}", registrationId);
                return encryptedPayload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating secure ticket data for registration {RegistrationId}", registrationId);
                throw;
            }
        }

        public async Task<bool> ValidateTicketDataAsync(string qrData)
        {
            try
            {
                var protector = _dataProtectionProvider.CreateProtector("TicketQRCode");
                var decryptedPayload = protector.Unprotect(qrData);
                var ticketPayload = JsonSerializer.Deserialize<TicketQRPayload>(decryptedPayload);

                if (ticketPayload == null)
                    return false;

                // Check expiration
                if (ticketPayload.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Expired ticket data for registration {RegistrationId}", ticketPayload.RegistrationId);
                    return false;
                }

                // Validate signature
                var expectedSignature = GenerateSignature(
                    ticketPayload.RegistrationId,
                    ticketPayload.TicketTypeId,
                    ticketPayload.UserEmail);

                if (ticketPayload.Signature != expectedSignature)
                {
                    _logger.LogWarning("Invalid signature for ticket registration {RegistrationId}", ticketPayload.RegistrationId);
                    return false;
                }

                // Check if registration exists and is valid
                var registration = await _context.Registrations
                    .FirstOrDefaultAsync(r => r.RegisterID == ticketPayload.RegistrationId &&
                                            r.AttendeeEmail == ticketPayload.UserEmail &&
                                            r.Status == RegistrationStatus.Confirmed);

                if (registration == null)
                {
                    _logger.LogWarning("Registration not found or invalid for ID {RegistrationId}", ticketPayload.RegistrationId);
                    return false;
                }

                _logger.LogInformation("Successfully validated ticket for registration {RegistrationId}", ticketPayload.RegistrationId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating ticket data: {QRData}", qrData);
                return false;
            }
        }
        public async Task<TicketQRPayload?> ExtractTicketPayloadAsync(string qrData)
        {
            try
            {
                var protector = _dataProtectionProvider.CreateProtector("TicketQRCode");
                var decryptedPayload = protector.Unprotect(qrData);
                var ticketPayload = JsonSerializer.Deserialize<TicketQRPayload>(decryptedPayload);

                return ticketPayload;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ticket payload from QR data");
                return null;
            }
        }

        public async Task<byte[]> GenerateQRCodeImageAsync(string qrData)
        {
            // Reuse the existing GenerateTicketQRCodeAsync method
            return await GenerateTicketQRCodeAsync(qrData);
        }

        private string GenerateSignature(int registrationId, int ticketTypeId, string userEmail)
        {
            var data = $"{registrationId}:{ticketTypeId}:{userEmail}:{DateTime.UtcNow:yyyyMMdd}";
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }
    }

    // Helper class for QR code payload
    public class TicketQRPayload
    {
        public int RegistrationId { get; set; }
        public int TicketTypeId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Signature { get; set; } = string.Empty;
    }
}