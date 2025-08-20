using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Services
{
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            ILogger<TicketService> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _logger = logger;
        }

        public async Task<ApiResponse<CheckInTicketResponse>> CheckInTicketAsync(CheckInTicketRequest request, int checkInUserId)
        {
            try
            {
                _logger.LogInformation("🎫 Starting ticket check-in process for QR data");

                // Step 1: Validate QR code
                var isValidQR = await _qrCodeService.ValidateTicketDataAsync(request.QRCodeData);
                if (!isValidQR)
                {
                    return ApiResponse<CheckInTicketResponse>.ErrorResult("Invalid or expired QR code");
                }

                // Step 2: Extract ticket information from QR code
                var ticketPayload = await ExtractTicketPayloadAsync(request.QRCodeData);
                if (ticketPayload == null)
                {
                    return ApiResponse<CheckInTicketResponse>.ErrorResult("Unable to read ticket information");
                }

                // Step 3: Find the issued ticket
                var issuedTicket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                    .Include(it => it.TicketType)
                    .FirstOrDefaultAsync(it => it.RegisterID == ticketPayload.RegistrationId &&
                                             it.TicketTypeID == ticketPayload.TicketTypeId &&
                                             it.AttendeeEmail == ticketPayload.UserEmail);

                if (issuedTicket == null)
                {
                    return ApiResponse<CheckInTicketResponse>.ErrorResult("Ticket not found");
                }

                // Step 4: Validate ticket status
                var validationResult = ValidateTicketForCheckIn(issuedTicket);
                if (!validationResult.IsValid)
                {
                    return ApiResponse<CheckInTicketResponse>.ErrorResult(validationResult.Message);
                }

                // Step 5: Perform check-in
                issuedTicket.CheckedInAt = DateTime.UtcNow;
                issuedTicket.CheckedInByUserID = checkInUserId;
                issuedTicket.Status = TicketStatus.Used;

                // Update registration status if this is the first ticket checked in
                var registration = issuedTicket.Registration;
                if (registration.Status == RegistrationStatus.Confirmed)
                {
                    registration.Status = RegistrationStatus.CheckedIn;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ticket checked in successfully: {TicketCode}", issuedTicket.UniqueReferenceCode);

                // Step 6: Prepare response
                var response = new CheckInTicketResponse
                {
                    Success = true,
                    Message = "Ticket checked in successfully",
                    TicketDetails = MapToTicketDetails(issuedTicket)
                };

                return ApiResponse<CheckInTicketResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during ticket check-in");
                return ApiResponse<CheckInTicketResponse>.ErrorResult("An error occurred during check-in", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<ValidateTicketResponse>> ValidateTicketAsync(ValidateTicketRequest request)
        {
            try
            {
                var response = new ValidateTicketResponse();

                // Validate QR code
                var isValidQR = await _qrCodeService.ValidateTicketDataAsync(request.QRCodeData);
                if (!isValidQR)
                {
                    response.IsValid = false;
                    response.Message = "Invalid or expired QR code";
                    response.ValidationErrors.Add("QR code validation failed");
                    return ApiResponse<ValidateTicketResponse>.SuccessResult(response);
                }

                // Extract and find ticket
                var ticketPayload = await ExtractTicketPayloadAsync(request.QRCodeData);
                if (ticketPayload == null)
                {
                    response.IsValid = false;
                    response.Message = "Unable to read ticket information";
                    return ApiResponse<ValidateTicketResponse>.SuccessResult(response);
                }

                var issuedTicket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                    .Include(it => it.TicketType)
                    .Include(it => it.CheckedInByUser)
                    .FirstOrDefaultAsync(it => it.RegisterID == ticketPayload.RegistrationId &&
                                             it.TicketTypeID == ticketPayload.TicketTypeId &&
                                             it.AttendeeEmail == ticketPayload.UserEmail);

                if (issuedTicket == null)
                {
                    response.IsValid = false;
                    response.Message = "Ticket not found";
                    return ApiResponse<ValidateTicketResponse>.SuccessResult(response);
                }

                // Validate ticket
                var validationResult = ValidateTicketForCheckIn(issuedTicket);
                response.IsValid = validationResult.IsValid;
                response.Message = validationResult.Message;
                response.ValidationErrors = validationResult.Errors;
                response.TicketDetails = MapToTicketDetails(issuedTicket);

                return ApiResponse<ValidateTicketResponse>.SuccessResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating ticket");
                return ApiResponse<ValidateTicketResponse>.ErrorResult("An error occurred during validation", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<TicketCheckInDetails>> GetTicketDetailsByCodeAsync(string ticketCode)
        {
            try
            {
                var issuedTicket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                    .Include(it => it.TicketType)
                    .Include(it => it.CheckedInByUser)
                    .FirstOrDefaultAsync(it => it.UniqueReferenceCode == ticketCode);

                if (issuedTicket == null)
                {
                    return ApiResponse<TicketCheckInDetails>.ErrorResult("Ticket not found");
                }

                var ticketDetails = MapToTicketDetails(issuedTicket);
                return ApiResponse<TicketCheckInDetails>.SuccessResult(ticketDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving ticket details for code: {TicketCode}", ticketCode);
                return ApiResponse<TicketCheckInDetails>.ErrorResult("An error occurred while retrieving ticket details", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<TicketCheckInDetails>>> GetEventCheckInsAsync(int eventId)
        {
            try
            {
                var checkedInTickets = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                    .Include(it => it.TicketType)
                    .Include(it => it.CheckedInByUser)
                    .Where(it => it.Registration.EventID == eventId && it.CheckedInAt.HasValue)
                    .OrderByDescending(it => it.CheckedInAt)
                    .ToListAsync();

                var ticketDetailsList = checkedInTickets.Select(MapToTicketDetails).ToList();
                return ApiResponse<List<TicketCheckInDetails>>.SuccessResult(ticketDetailsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving check-ins for event: {EventId}", eventId);
                return ApiResponse<List<TicketCheckInDetails>>.ErrorResult("An error occurred while retrieving check-ins", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> UndoCheckInAsync(int ticketId, int userId)
        {
            try
            {
                var issuedTicket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                    .FirstOrDefaultAsync(it => it.IssuedTicketID == ticketId);

                if (issuedTicket == null)
                {
                    return ApiResponse<bool>.ErrorResult("Ticket not found");
                }

                if (!issuedTicket.CheckedInAt.HasValue)
                {
                    return ApiResponse<bool>.ErrorResult("Ticket is not checked in");
                }

                // Undo check-in
                issuedTicket.CheckedInAt = null;
                issuedTicket.CheckedInByUserID = null;
                issuedTicket.Status = TicketStatus.Valid;

                // Check if we need to update registration status back
                var registration = issuedTicket.Registration;
                var otherCheckedInTickets = await _context.IssuedTickets
                    .Where(it => it.RegisterID == registration.RegisterID &&
                               it.IssuedTicketID != ticketId &&
                               it.CheckedInAt.HasValue)
                    .AnyAsync();

                if (!otherCheckedInTickets)
                {
                    registration.Status = RegistrationStatus.Confirmed;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("🔄 Check-in undone for ticket: {TicketId} by user: {UserId}", ticketId, userId);
                return ApiResponse<bool>.SuccessResult(true, "Check-in undone successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error undoing check-in for ticket: {TicketId}", ticketId);
                return ApiResponse<bool>.ErrorResult("An error occurred while undoing check-in", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<UserTicketDto>>> GetUserTicketsAsync(int userId)
        {
            try
            {
                _logger.LogInformation("🎫 Retrieving tickets for user: {UserId}", userId);

                var tickets = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(it => it.TicketType)
                    .Where(it => it.Registration.UserID == userId)
                    .OrderByDescending(it => it.Registration.Event.StartDate)
                    .Select(it => new UserTicketDto
                    {
                        IssuedTicketID = it.IssuedTicketID,
                        UniqueReferenceCode = it.UniqueReferenceCode,
                        QRCodeData = it.QRCodeData,
                        TicketTypeName = it.TicketType.TypeName,
                        Price = it.TicketType.Price,
                        AttendeeName = it.AttendeeName,
                        AttendeeEmail = it.AttendeeEmail,
                        CheckedInAt = it.CheckedInAt,
                        Status = it.Status,
                        IssuedAt = it.IssuedAt ?? DateTime.Now,
                        
                        // Event Details
                        EventID = it.Registration.Event.EventID,
                        EventName = it.Registration.Event.EventName,
                        EventSlug = it.Registration.Event.UrlSlug,
                        EventStartDate = it.Registration.Event.StartDate,
                        EventEndDate = it.Registration.Event.EndDate ?? DateTime.MinValue,
                        EventStatus = it.Registration.Event.Status,
                        VenueName = it.Registration.Event.Venue.VenueName,
                        VenueAddress = it.Registration.Event.Venue.Address,
                        EventImageUrl = it.Registration.Event.ImageUrl,
                        
                        // Registration Details
                        RegistrationID = it.Registration.RegisterID,
                        RegisteredAt = it.Registration.RegisteredAt ?? DateTime.Now,
                        RegistrationStatus = it.Registration.Status
                    })
                    .ToListAsync();

                _logger.LogInformation("✅ Retrieved {TicketCount} tickets for user: {UserId}", tickets.Count, userId);
                return ApiResponse<List<UserTicketDto>>.SuccessResult(tickets, $"Retrieved {tickets.Count} tickets");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving tickets for user: {UserId}", userId);
                return ApiResponse<List<UserTicketDto>>.ErrorResult("An error occurred while retrieving your tickets", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<TicketQRPayload?> ExtractTicketPayloadAsync(string qrData)
        {
            return await _qrCodeService.ExtractTicketPayloadAsync(qrData);
        }
        private (bool IsValid, string Message, List<string> Errors) ValidateTicketForCheckIn(IssuedTicket ticket)
        {
            var errors = new List<string>();

            // Check if ticket is valid
            if (ticket.Status == TicketStatus.Cancelled)
            {
                errors.Add("Ticket has been cancelled");
            }

            if (ticket.Status == TicketStatus.Expired)
            {
                errors.Add("Ticket has expired");
            }

            if (ticket.Status == TicketStatus.Used)
            {
                errors.Add($"Ticket already checked in at {ticket.CheckedInAt:yyyy-MM-dd HH:mm}");
            }

            // Check if registration is valid
            if (ticket.Registration.Status == RegistrationStatus.Cancelled)
            {
                errors.Add("Registration has been cancelled");
            }

            // Check if event is today or has started
            var eventDate = ticket.Registration.Event.StartDate.Date;
            var today = DateTime.Now.Date;

            if (eventDate > today.AddDays(1)) // Allow check-in 1 day early
            {
                errors.Add("Event hasn't started yet");
            }

            if (eventDate < today.AddDays(-1)) // Allow check-in 1 day after
            {
                errors.Add("Event has ended");
            }

            var isValid = !errors.Any();
            var message = isValid ? "Ticket is valid for check-in" : string.Join("; ", errors);

            return (isValid, message, errors);
        }

        private TicketCheckInDetails MapToTicketDetails(IssuedTicket ticket)
        {
            return new TicketCheckInDetails
            {
                IssuedTicketID = ticket.IssuedTicketID,
                UniqueReferenceCode = ticket.UniqueReferenceCode,
                AttendeeName = ticket.AttendeeName,
                AttendeeEmail = ticket.AttendeeEmail,
                EventName = ticket.Registration?.Event?.EventName ?? "Unknown Event",
                TicketTypeName = ticket.TicketType?.TypeName ?? "Unknown Type",
                CheckedInAt = ticket.CheckedInAt,
                CheckedInByUser = ticket.CheckedInByUser?.Name ?? "Unknown",
                Status = ticket.Status
            };
        }

        #endregion
    }
}