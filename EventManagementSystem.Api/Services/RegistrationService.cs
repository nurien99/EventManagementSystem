using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace EventManagementSystem.Api.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(ApplicationDbContext context, INotificationService notificationService, IQRCodeService qrCodeService, ILogger<RegistrationService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _qrCodeService = qrCodeService;
            _logger = logger;  
        }

        public async Task<ApiResponse<RegistrationDto>> RegisterForEventAsync(CreateRegistrationDto registrationDto)
        {
            try
            {
                // Validate event exists and is published
                var eventEntity = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.TicketTypes)
                    .FirstOrDefaultAsync(e => e.EventID == registrationDto.EventID);

                if (eventEntity == null)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("Event not found.");
                }

                if (eventEntity.Status != EventStatus.Published)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("Event is not available for registration.");
                }

                // Check if registration deadline has passed
                if (eventEntity.RegistrationDeadline.HasValue &&
                    DateTime.UtcNow > eventEntity.RegistrationDeadline.Value)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("Registration deadline has passed.");
                }

                // Validate ticket selections
                if (!registrationDto.TicketSelections.Any())
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("At least one ticket must be selected.");
                }

                var totalTicketsRequested = registrationDto.TicketSelections.Sum(ts => ts.Quantity);
                if (totalTicketsRequested <= 0)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("Invalid ticket quantity.");
                }

                // Validate ticket types and availability
                var ticketTypeIds = registrationDto.TicketSelections.Select(ts => ts.TicketTypeID).ToList();
                var availableTicketTypes = eventEntity.TicketTypes
                    .Where(tt => ticketTypeIds.Contains(tt.TicketTypeID) && tt.IsActive)
                    .ToList();

                if (availableTicketTypes.Count != ticketTypeIds.Count)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("One or more selected ticket types are invalid or inactive.");
                }

                // Check ticket availability and sale period for each type
                foreach (var selection in registrationDto.TicketSelections)
                {
                    var ticketType = availableTicketTypes.First(tt => tt.TicketTypeID == selection.TicketTypeID);

                    // Check if tickets are currently on sale
                    if (!ticketType.IsSaleActive)
                    {
                        return ApiResponse<RegistrationDto>.ErrorResult($"Ticket type '{ticketType.TypeName}' is not currently on sale.");
                    }

                    var availableQuantity = ticketType.AvailableQuantity;
                    if (selection.Quantity > availableQuantity)
                    {
                        return ApiResponse<RegistrationDto>.ErrorResult($"Only {availableQuantity} tickets available for '{ticketType.TypeName}'.");
                    }
                }

                // Check event capacity
                if (eventEntity.MaxCapacity.HasValue)
                {
                    var currentRegistrations = await _context.Registrations
                        .Where(r => r.EventID == registrationDto.EventID &&
                               (r.Status == RegistrationStatus.Confirmed || r.Status == RegistrationStatus.CheckedIn))
                        .SelectMany(r => r.IssuedTickets)
                        .CountAsync();

                    if (currentRegistrations + totalTicketsRequested > eventEntity.MaxCapacity.Value)
                    {
                        return ApiResponse<RegistrationDto>.ErrorResult("Event capacity would be exceeded.");
                    }
                }

                // Get or validate user
                int userId;
                if (registrationDto.UserID.HasValue)
                {
                    // Registered user
                    userId = registrationDto.UserID.Value;
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null || !user.IsActive)
                    {
                        return ApiResponse<RegistrationDto>.ErrorResult("Invalid user.");
                    }

                    // Check if user is already registered for this event
                    var existingRegistration = await _context.Registrations
                        .FirstOrDefaultAsync(r => r.UserID == userId && r.EventID == registrationDto.EventID &&
                                                r.Status != RegistrationStatus.Cancelled);

                    if (existingRegistration != null)
                    {
                        return ApiResponse<RegistrationDto>.ErrorResult("You are already registered for this event.");
                    }
                }
                else
                {
                    // Guest registration - create a guest user account
                    var existingGuestUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == registrationDto.AttendeeEmail);

                    if (existingGuestUser != null)
                    {
                        userId = existingGuestUser.UserID;

                        // Check if already registered
                        var existingRegistration = await _context.Registrations
                            .FirstOrDefaultAsync(r => r.UserID == userId && r.EventID == registrationDto.EventID &&
                                                    r.Status != RegistrationStatus.Cancelled);

                        if (existingRegistration != null)
                        {
                            return ApiResponse<RegistrationDto>.ErrorResult("An account with this email is already registered for this event.");
                        }
                    }
                    else
                    {
                        // Create new guest user
                        var guestUser = new User
                        {
                            Name = registrationDto.AttendeeName,
                            Email = registrationDto.AttendeeEmail,
                            Password = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password for guest
                            Role = UserRole.Attendee,
                            PhoneNumber = registrationDto.AttendeePhone ?? string.Empty,
                            Organization = registrationDto.AttendeeOrganization ?? string.Empty,
                            IsEmailVerified = false,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Users.Add(guestUser);
                        await _context.SaveChangesAsync();
                        userId = guestUser.UserID;
                    }
                }

                // Create registration
                var registration = new Registration
                {
                    UserID = userId,
                    EventID = registrationDto.EventID,
                    Status = RegistrationStatus.Confirmed,
                    RegisteredAt = DateTime.UtcNow,
                    AttendeeName = registrationDto.AttendeeName,
                    AttendeeEmail = registrationDto.AttendeeEmail,
                    AttendeePhone = registrationDto.AttendeePhone,
                    AttendeeOrganization = registrationDto.AttendeeOrganization,
                    SpecialRequirements = registrationDto.SpecialRequirements,
                    CancellationReason = string.Empty
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                var issuedTickets = new List<IssuedTicket>();
                // Create issued tickets for each ticket selection
                foreach (var selection in registrationDto.TicketSelections)
                {
                    var ticketType = availableTicketTypes.First(tt => tt.TicketTypeID == selection.TicketTypeID);

                    for (int i = 0; i < selection.Quantity; i++)
                    {
                        // ✅ GENERATE SECURE QR CODE DATA
                        var secureQRData = await _qrCodeService.GenerateSecureTicketDataAsync(
                            registration.RegisterID,
                            selection.TicketTypeID,
                            registrationDto.AttendeeEmail
                        );

                        var issuedTicket = new IssuedTicket
                        {
                            RegisterID = registration.RegisterID,
                            TicketTypeID = selection.TicketTypeID,
                            UniqueReferenceCode = await GenerateUniqueTicketCodeAsync(),
                            QRCodeData = secureQRData, // ✅ USE SECURE DATA INSTEAD
                            AttendeeName = registrationDto.AttendeeName,
                            AttendeeEmail = registrationDto.AttendeeEmail,
                            AttendeePhoneNo = registrationDto.AttendeePhone ?? string.Empty,
                            AttendeeOrganization = registrationDto.AttendeeOrganization ?? string.Empty,
                            Status = TicketStatus.Valid,
                            IssuedAt = DateTime.UtcNow
                        };

                        issuedTickets.Add(issuedTicket);
                    }

                    // Update sold quantity
                    ticketType.SoldQuantity += selection.Quantity;
                    ticketType.UpdatedAt = DateTime.UtcNow;
                }

                _context.IssuedTickets.AddRange(issuedTickets);
                await _context.SaveChangesAsync();
                // ✅ ADD THIS - Send registration confirmation email
                try
                {
                    _logger.LogInformation("📧 Sending registration confirmation email for registration {RegistrationId}", registration.RegisterID);

                    var emailResult = await _notificationService.SendRegistrationConfirmationAsync(registration.RegisterID);

                    if (emailResult.Success)
                    {
                        _logger.LogInformation("✅ Registration confirmation email queued successfully for registration {RegistrationId}", registration.RegisterID);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to queue registration confirmation email for registration {RegistrationId}: {Message}",
                            registration.RegisterID, emailResult.Message);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "❌ Exception occurred while sending registration confirmation email for registration {RegistrationId}", registration.RegisterID);
                    // Don't fail the registration if email fails
                }

                // Return registration with full details
                var registrationDto_result = await GetRegistrationWithDetailsAsync(registration.RegisterID);
                return ApiResponse<RegistrationDto>.SuccessResult(registrationDto_result, "Registration successful.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during registration for event {EventId}", registrationDto.EventID);
                return ApiResponse<RegistrationDto>.ErrorResult("An error occurred during registration.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<RegistrationDto>>> GetUserRegistrationsAsync(int userId)
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .Where(r => r.UserID == userId)
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToListAsync();

                var registrationDtos = registrations.Select(MapToRegistrationDto).ToList();
                return ApiResponse<List<RegistrationDto>>.SuccessResult(registrationDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<RegistrationDto>>.ErrorResult("An error occurred while retrieving registrations.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<RegistrationDto>>> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.User)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .Where(r => r.EventID == eventId)
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToListAsync();

                var registrationDtos = registrations.Select(MapToRegistrationDto).ToList();
                return ApiResponse<List<RegistrationDto>>.SuccessResult(registrationDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<RegistrationDto>>.ErrorResult("An error occurred while retrieving event registrations.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> CancelRegistrationAsync(int registrationId, int userId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Event)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId && r.UserID == userId);

                if (registration == null)
                {
                    return ApiResponse<bool>.ErrorResult("Registration not found.");
                }

                if (registration.Status == RegistrationStatus.Cancelled)
                {
                    return ApiResponse<bool>.ErrorResult("Registration is already cancelled.");
                }

                // Check if event has already started
                if (DateTime.UtcNow >= registration.Event.StartDate)
                {
                    return ApiResponse<bool>.ErrorResult("Cannot cancel registration for events that have already started.");
                }

                // Update registration status
                registration.Status = RegistrationStatus.Cancelled;
                registration.CancelledAt = DateTime.UtcNow;
                registration.CancellationReason = "Cancelled by user";

                // Cancel all issued tickets
                foreach (var ticket in registration.IssuedTickets)
                {
                    ticket.Status = TicketStatus.Cancelled;
                }

                // Update ticket sold quantities
                var ticketTypeUpdates = registration.IssuedTickets
                    .GroupBy(it => it.TicketTypeID)
                    .ToList();

                foreach (var group in ticketTypeUpdates)
                {
                    var ticketType = group.First().TicketType;
                    ticketType.SoldQuantity -= group.Count();
                    ticketType.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Registration cancelled successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while cancelling the registration.", new List<string> { ex.Message });
            }
        }
        public async Task<ApiResponse<List<RegistrationDto>>> GetRegistrationsByEmailAsync(string email)
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .Where(r => r.AttendeeEmail.ToLower() == email.ToLower())
                    .OrderByDescending(r => r.RegisteredAt)
                    .ToListAsync();

                var registrationDtos = registrations.Select(MapToRegistrationDto).ToList();
                return ApiResponse<List<RegistrationDto>>.SuccessResult(registrationDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<RegistrationDto>>.ErrorResult("An error occurred while retrieving registrations.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<RegistrationDto>> GetRegistrationByEmailAndIdAsync(string email, int registrationId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId &&
                                            r.AttendeeEmail.ToLower() == email.ToLower());

                if (registration == null)
                {
                    return ApiResponse<RegistrationDto>.ErrorResult("Registration not found.");
                }

                var registrationDto = MapToRegistrationDto(registration);
                return ApiResponse<RegistrationDto>.SuccessResult(registrationDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<RegistrationDto>.ErrorResult("An error occurred while retrieving the registration.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> CancelRegistrationByEmailAsync(string email, int registrationId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Event)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId &&
                                            r.AttendeeEmail.ToLower() == email.ToLower());

                if (registration == null)
                {
                    return ApiResponse<bool>.ErrorResult("Registration not found.");
                }

                if (registration.Status == RegistrationStatus.Cancelled)
                {
                    return ApiResponse<bool>.ErrorResult("Registration is already cancelled.");
                }

                // Check if event has already started
                if (DateTime.UtcNow >= registration.Event.StartDate)
                {
                    return ApiResponse<bool>.ErrorResult("Cannot cancel registration for events that have already started.");
                }

                // Update registration status
                registration.Status = RegistrationStatus.Cancelled;
                registration.CancelledAt = DateTime.UtcNow;
                registration.CancellationReason = "Cancelled by attendee via email lookup";

                // Cancel all issued tickets
                foreach (var ticket in registration.IssuedTickets)
                {
                    ticket.Status = TicketStatus.Cancelled;
                }

                // Update ticket sold quantities
                var ticketTypeUpdates = registration.IssuedTickets
                    .GroupBy(it => it.TicketTypeID)
                    .ToList();

                foreach (var group in ticketTypeUpdates)
                {
                    var ticketType = group.First().TicketType;
                    ticketType.SoldQuantity -= group.Count();
                    ticketType.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Registration cancelled successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while cancelling the registration.", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<RegistrationDto> GetRegistrationWithDetailsAsync(int registrationId)
        {
            var registration = await _context.Registrations
                .Include(r => r.Event)
                    .ThenInclude(e => e.Venue)
                .Include(r => r.IssuedTickets)
                    .ThenInclude(it => it.TicketType)
                .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

            return registration != null ? MapToRegistrationDto(registration) : null;
        }

        private RegistrationDto MapToRegistrationDto(Registration registration)
        {
            return new RegistrationDto
            {
                RegisterID = registration.RegisterID,
                UserID = registration.UserID,
                EventID = registration.EventID,
                EventName = registration.Event?.EventName ?? "Unknown",
                Status = registration.Status,
                RegisteredAt = registration.RegisteredAt,
                AttendeeName = registration.AttendeeName,
                AttendeeEmail = registration.AttendeeEmail,
                AttendeePhone = registration.AttendeePhone,
                AttendeeOrganization = registration.AttendeeOrganization,
                SpecialRequirements = registration.SpecialRequirements,
                IssuedTickets = registration.IssuedTickets?.Select(MapToIssuedTicketDto).ToList() ?? new List<IssuedTicketDto>()
            };
        }

        private IssuedTicketDto MapToIssuedTicketDto(IssuedTicket issuedTicket)
        {
            return new IssuedTicketDto
            {
                IssuedTicketID = issuedTicket.IssuedTicketID,
                UniqueReferenceCode = issuedTicket.UniqueReferenceCode,
                QRCodeData = issuedTicket.QRCodeData,
                TicketTypeName = issuedTicket.TicketType?.TypeName ?? "Unknown",
                AttendeeName = issuedTicket.AttendeeName,
                AttendeeEmail = issuedTicket.AttendeeEmail,
                CheckedInAt = issuedTicket.CheckedInAt,
                Status = issuedTicket.Status,
                IssuedAt = issuedTicket.IssuedAt
            };
        }

        private async Task<string> GenerateUniqueTicketCodeAsync()
        {
            string code;
            do
            {
                code = GenerateTicketCode();
            }
            while (await _context.IssuedTickets.AnyAsync(t => t.UniqueReferenceCode == code));

            return code;
        }

        private string GenerateTicketCode()
        {
            // Generate a unique 12-character alphanumeric code
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion
    }
}