using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Api.Models;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core.DTOs.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventManagementSystem.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<NotificationService> _logger;
        private readonly FrontendSettings _frontendSettings;

        public NotificationService(
            ApplicationDbContext context,
            IEmailService emailService,
            IQRCodeService qrCodeService,
            ILogger<NotificationService> logger,
            IOptions<FrontendSettings> frontendSettings)
        {
            _context = context;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _logger = logger;
            _frontendSettings = frontendSettings.Value;
        }

        #region User Account Notifications

        public async Task<ApiResponse<bool>> SendWelcomeEmailAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                var welcomeModel = new UserWelcomeModel
                {
                    UserName = user.Name,
                    Email = user.Email,
                    SiteName = _frontendSettings.AppName,
                    SiteUrl = _frontendSettings.BaseUrl,
                    // Use frontend verification page that calls API
                    EmailVerificationUrl = $"{_frontendSettings.BaseUrl}/verify-email?token={user.EmailVerificationToken}&email={Uri.EscapeDataString(user.Email)}",
                    LoginUrl = $"{_frontendSettings.BaseUrl}/login",
                    SentAt = DateTime.UtcNow
                };

                var result = await _emailService.SendTemplateEmailAsync(
                    user.Email,
                    $"Welcome to Event Management System, {user.Name}!",
                    "UserWelcome",
                    welcomeModel
                );

                if (result.Success)
                {
                    _logger.LogInformation("✅ Welcome email sent successfully to user {UserId} ({Email})", userId, user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending welcome email to user {UserId}", userId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending welcome email", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendEmailVerificationAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found");
                }

                if (user.IsEmailVerified)
                {
                    return ApiResponse<bool>.SuccessResult(true, "Email is already verified");
                }

                var verificationModel = new UserWelcomeModel // Reusing the model for simplicity
                {
                    UserName = user.Name,
                    Email = user.Email,
                    SiteName = _frontendSettings.AppName,
                    SiteUrl = _frontendSettings.BaseUrl,
                    EmailVerificationUrl = $"{_frontendSettings.BaseUrl}/verify-email?token={user.EmailVerificationToken}&email={Uri.EscapeDataString(user.Email)}",
                    LoginUrl = $"{_frontendSettings.BaseUrl}/login",
                    SentAt = DateTime.UtcNow
                };

                var result = await _emailService.SendTemplateEmailAsync(
                    user.Email,
                    "Please verify your email address",
                    "EmailVerification",
                    verificationModel
                );

                if (result.Success)
                {
                    _logger.LogInformation("📧 Email verification sent to user {UserId} ({Email})", userId, user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending email verification to user {UserId}", userId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending email verification", new List<string> { ex.Message });
            }
        }

        #endregion

        #region Event-Related Notifications

        // Fix the SendRegistrationConfirmationAsync method in your NotificationService.cs

        public async Task<ApiResponse<bool>> SendRegistrationConfirmationAsync(int registrationId)
        {
            try
            {
                _logger.LogInformation("📧 Starting registration confirmation email for registration {RegistrationId}", registrationId);

                var registration = await _context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Category)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

                if (registration == null)
                {
                    _logger.LogError("❌ Registration not found for ID: {RegistrationId}", registrationId);
                    return ApiResponse<bool>.ErrorResult("Registration not found");
                }

                // Check if there are issued tickets
                if (registration.IssuedTickets == null || !registration.IssuedTickets.Any())
                {
                    _logger.LogError("❌ No issued tickets found for registration {RegistrationId}", registrationId);
                    return ApiResponse<bool>.ErrorResult("No issued tickets found for registration");
                }

                var primaryTicket = registration.IssuedTickets.FirstOrDefault();
                if (primaryTicket == null)
                {
                    _logger.LogError("❌ Primary ticket is null for registration {RegistrationId}", registrationId);
                    return ApiResponse<bool>.ErrorResult("Primary ticket not found");
                }

                _logger.LogInformation("🎫 Found primary ticket {TicketCode} with type {TicketTypeId}",
                    primaryTicket.UniqueReferenceCode, primaryTicket.TicketTypeID);

                // ✅ Generate QR code as Base64 string - THIS IS THE WORKING VERSION
                string qrCodeBase64 = string.Empty;
                try
                {
                    _logger.LogInformation("🔄 Generating secure ticket data for registration {RegistrationId}", registrationId);

                    var qrData = await _qrCodeService.GenerateSecureTicketDataAsync(
                        registration.RegisterID,
                        primaryTicket.TicketTypeID,
                        registration.AttendeeEmail
                    );

                    _logger.LogInformation("✅ Secure ticket data generated, length: {Length}", qrData.Length);

                    var qrCodeBytes = await _qrCodeService.GenerateTicketQRCodeAsync(qrData);
                    qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

                    _logger.LogInformation("✅ QR code generated successfully, Base64 length: {Length}", qrCodeBase64.Length);
                }
                catch (Exception qrEx)
                {
                    _logger.LogError(qrEx, "❌ Failed to generate QR code for registration {RegistrationId}", registrationId);
                    // Continue without QR code rather than failing the entire email
                    qrCodeBase64 = string.Empty;
                }

                // Prepare ticket type information
                var ticketTypes = registration.IssuedTickets
                    .Where(it => it.TicketType != null)
                    .GroupBy(it => it.TicketType)
                    .Select(g => new TicketTypeInfo
                    {
                        TypeName = g.Key.TypeName,
                        Quantity = g.Count(),
                        Price = g.Key.Price
                    })
                    .ToList();

                // ✅ Create confirmation model with ALL required properties
                var confirmationModel = new RegistrationConfirmationModel
                {
                    UserName = registration.AttendeeName,
                    Email = registration.AttendeeEmail, // ✅ This was missing before!
                    EventTitle = registration.Event.EventName,
                    EventDescription = registration.Event.EventDesc ?? string.Empty,
                    EventStartDate = registration.Event.StartDate,
                    EventEndDate = registration.Event.EndDate,
                    VenueName = registration.Event.Venue.VenueName,
                    VenueAddress = registration.Event.Venue.Address,
                    TicketNumber = primaryTicket.UniqueReferenceCode, // ✅ Use actual ticket code
                    QRCodeBase64 = qrCodeBase64, // ✅ This should now work!
                    TicketTypes = ticketTypes,
                    RegistrationId = registration.RegisterID.ToString(),
                    CancellationUrl = $"https://localhost:7203/cancel-registration?id={registration.RegisterID}&email={Uri.EscapeDataString(registration.AttendeeEmail)}",
                    SiteName = "Event Management System" // ✅ This was also missing!
                };

                // 🔍 DEBUG LOG - Check what we're sending
                _logger.LogInformation("📬 Email model prepared:");
                _logger.LogInformation("   UserName: {UserName}", confirmationModel.UserName);
                _logger.LogInformation("   Email: {Email}", confirmationModel.Email);
                _logger.LogInformation("   QRCodeBase64 Length: {Length}", confirmationModel.QRCodeBase64?.Length ?? 0);
                _logger.LogInformation("   QRCodeBase64 Empty: {IsEmpty}", string.IsNullOrEmpty(confirmationModel.QRCodeBase64));
                _logger.LogInformation("   SiteName: {SiteName}", confirmationModel.SiteName);

                var result = await _emailService.SendTemplateEmailAsync(
                    registration.AttendeeEmail,
                    $"🎫 Registration Confirmed: {registration.Event.EventName}",
                    "RegistrationConfirmation",
                    confirmationModel
                );

                if (result.Success)
                {
                    _logger.LogInformation("✅ Registration confirmation sent successfully for registration {RegistrationId} to {Email}",
                        registrationId, registration.AttendeeEmail);
                }
                else
                {
                    _logger.LogError("❌ Failed to send registration confirmation for registration {RegistrationId}: {Message}",
                        registrationId, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending registration confirmation for registration {RegistrationId}", registrationId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending registration confirmation", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendEventReminderAsync(int eventId, int hoursBeforeEvent)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.Registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    return ApiResponse<bool>.ErrorResult("Event not found");
                }

                var timeUntilEvent = eventEntity.StartDate - DateTime.UtcNow;
                var hoursUntilEvent = $"{Math.Ceiling(timeUntilEvent.TotalHours)} hours";

                if (timeUntilEvent.TotalDays >= 1)
                {
                    var days = Math.Ceiling(timeUntilEvent.TotalDays);
                    hoursUntilEvent = $"{days} day{(days != 1 ? "s" : "")}";
                }

                var successCount = 0;
                var failureCount = 0;

                foreach (var registration in eventEntity.Registrations)
                {
                    try
                    {
                        var reminderModel = new EventReminderModel
                        {
                            UserName = registration.AttendeeName,
                            EventTitle = eventEntity.EventName,
                            EventStartDate = eventEntity.StartDate,
                            VenueName = eventEntity.Venue.VenueName,
                            VenueAddress = eventEntity.Venue.Address,
                            EventUrl = $"https://localhost:7203/events/{eventEntity.UrlSlug}",
                            HoursUntilEvent = hoursUntilEvent,
                            SpecialInstructions = "Please arrive 15 minutes early for check-in. Don't forget to bring your ticket QR code!"
                        };

                        var result = await _emailService.SendTemplateEmailAsync(
                            registration.AttendeeEmail,
                            $"⏰ Reminder: {eventEntity.EventName} starts in {hoursUntilEvent}",
                            "EventReminder",
                            reminderModel
                        );

                        if (result.Success)
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error sending reminder to {Email} for event {EventId}", registration.AttendeeEmail, eventId);
                        failureCount++;
                    }
                }

                _logger.LogInformation("⏰ Event reminder sent for event {EventId}: {SuccessCount} successful, {FailureCount} failed",
                    eventId, successCount, failureCount);

                return ApiResponse<bool>.SuccessResult(true, $"Reminder sent to {successCount} attendees, {failureCount} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending event reminders for event {EventId}", eventId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending event reminders", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendEventCancellationAsync(int eventId, string reason)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    return ApiResponse<bool>.ErrorResult("Event not found");
                }

                var successCount = 0;
                var failureCount = 0;

                foreach (var registration in eventEntity.Registrations)
                {
                    try
                    {
                        var cancellationModel = new EventCancellationModel
                        {
                            UserName = registration.AttendeeName,
                            EventTitle = eventEntity.EventName,
                            OriginalEventDate = eventEntity.StartDate,
                            CancellationReason = reason,
                            RefundInformation = "If you paid for this event, a full refund will be processed within 5-7 business days.",
                            ContactEmail = "support@eventmanagement.com"
                        };

                        var result = await _emailService.SendTemplateEmailAsync(
                            registration.AttendeeEmail,
                            $"❌ Event Cancelled: {eventEntity.EventName}",
                            "EventCancellation",
                            cancellationModel
                        );

                        if (result.Success)
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error sending cancellation notice to {Email} for event {EventId}", registration.AttendeeEmail, eventId);
                        failureCount++;
                    }
                }

                _logger.LogInformation("❌ Event cancellation sent for event {EventId}: {SuccessCount} successful, {FailureCount} failed",
                    eventId, successCount, failureCount);

                return ApiResponse<bool>.SuccessResult(true, $"Cancellation notice sent to {successCount} attendees, {failureCount} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending event cancellation for event {EventId}", eventId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending event cancellation", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendEventUpdateAsync(int eventId, string updateMessage)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.Registrations.Where(r => r.Status == RegistrationStatus.Confirmed))
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    return ApiResponse<bool>.ErrorResult("Event not found");
                }

                var successCount = 0;
                var failureCount = 0;

                foreach (var registration in eventEntity.Registrations)
                {
                    try
                    {
                        // Using reminder model for updates (can create a specific update model later)
                        var updateModel = new EventReminderModel
                        {
                            UserName = registration.AttendeeName,
                            EventTitle = eventEntity.EventName,
                            EventStartDate = eventEntity.StartDate,
                            VenueName = eventEntity.Venue.VenueName,
                            VenueAddress = eventEntity.Venue.Address,
                            EventUrl = $"https://localhost:7203/events/{eventEntity.UrlSlug}",
                            HoursUntilEvent = "update",
                            SpecialInstructions = updateMessage
                        };

                        var result = await _emailService.SendTemplateEmailAsync(
                            registration.AttendeeEmail,
                            $"📢 Important Update: {eventEntity.EventName}",
                            "EventReminder", // Reusing template for now
                            updateModel
                        );

                        if (result.Success)
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error sending update to {Email} for event {EventId}", registration.AttendeeEmail, eventId);
                        failureCount++;
                    }
                }

                _logger.LogInformation("📢 Event update sent for event {EventId}: {SuccessCount} successful, {FailureCount} failed",
                    eventId, successCount, failureCount);

                return ApiResponse<bool>.SuccessResult(true, $"Update sent to {successCount} attendees, {failureCount} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending event update for event {EventId}", eventId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending event update", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendTicketEmailAsync(int ticketId)
        {
            try
            {
                _logger.LogInformation("📧 Starting ticket email delivery for ticket {TicketId}", ticketId);

                var ticket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(it => it.TicketType)
                    .FirstOrDefaultAsync(it => it.IssuedTicketID == ticketId);

                if (ticket == null)
                {
                    _logger.LogError("❌ Ticket not found for ID: {TicketId}", ticketId);
                    return ApiResponse<bool>.ErrorResult("Ticket not found");
                }

                // Generate QR code as Base64 string
                string qrCodeBase64 = string.Empty;
                try
                {
                    qrCodeBase64 = await _qrCodeService.GenerateTicketQRCodeBase64Async(ticket.QRCodeData);
                    _logger.LogInformation("✅ QR code generated for ticket {TicketId}, length: {Length}", ticketId, qrCodeBase64.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error generating QR code for ticket {TicketId}", ticketId);
                }

                var ticketModel = new TicketDeliveryModel
                {
                    UserName = ticket.AttendeeName,
                    Email = ticket.AttendeeEmail,
                    SiteName = _frontendSettings.AppName,
                    SiteUrl = _frontendSettings.BaseUrl,
                    EventTitle = ticket.Registration.Event.EventName,
                    EventDescription = "", // Event entity doesn't have Description field
                    EventStartDate = ticket.Registration.Event.StartDate,
                    EventEndDate = ticket.Registration.Event.EndDate,
                    VenueName = ticket.Registration.Event.Venue?.VenueName ?? "TBD",
                    VenueAddress = ticket.Registration.Event.Venue?.Address ?? "TBD",
                    TicketNumber = ticket.UniqueReferenceCode,
                    TicketTypeName = ticket.TicketType?.TypeName ?? "General Admission",
                    TicketPrice = ticket.TicketType?.Price ?? 0,
                    AttendeeName = ticket.AttendeeName,
                    QRCodeBase64 = qrCodeBase64,
                    EventUrl = $"{_frontendSettings.BaseUrl}/events/{ticket.Registration.Event.UrlSlug}",
                    CheckInInstructions = "Please arrive at least 15 minutes before the event starts. Present your QR code at the entrance for quick check-in.",
                    SentAt = DateTime.UtcNow
                };

                var result = await _emailService.SendTemplateEmailAsync(
                    ticket.AttendeeEmail,
                    $"🎫 Your Ticket for {ticket.Registration.Event.EventName}",
                    "TicketDelivery",
                    ticketModel
                );

                if (result.Success)
                {
                    _logger.LogInformation("📧 Ticket email sent successfully for ticket {TicketId} to {Email}",
                        ticketId, ticket.AttendeeEmail);
                }
                else
                {
                    _logger.LogError("❌ Failed to send ticket email for ticket {TicketId}: {Message}",
                        ticketId, result.Message);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending ticket email for ticket {TicketId}", ticketId);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending ticket email", new List<string> { ex.Message });
            }
        }

        #endregion

        #region Bulk Notifications

        public async Task<ApiResponse<bool>> SendBulkEventRemindersAsync(int eventId)
        {
            return await SendEventReminderAsync(eventId, 24); // 24 hours before
        }

        public async Task<ApiResponse<bool>> SendBulkEventCancellationAsync(int eventId, string reason)
        {
            return await SendEventCancellationAsync(eventId, reason);
        }

        #endregion
    }
}