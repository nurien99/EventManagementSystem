using EventManagementSystem.Api.Data; // ✅ ADD THIS
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // ✅ ADD THIS
using System.Security.Claims;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ApplicationDbContext _context; // ✅ ADD THIS
        private readonly IQRCodeService _qrCodeService;
        private readonly IEventAssistantService _eventAssistantService; // ✅ ADD THIS
        private readonly INotificationService _notificationService;
        private readonly IPdfService _pdfService;

        // ✅ UPDATE CONSTRUCTOR
        public TicketsController(
            ITicketService ticketService,
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            IEventAssistantService eventAssistantService,
            INotificationService notificationService,
            IPdfService pdfService)
        {
            _ticketService = ticketService;
            _context = context;
            _qrCodeService = qrCodeService;
            _eventAssistantService = eventAssistantService;
            _notificationService = notificationService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Check in a ticket using QR code data
        /// </summary>
        [HttpPost("checkin")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<CheckInTicketResponse>>> CheckInTicket([FromBody] CheckInTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<CheckInTicketResponse>.ErrorResult("Validation failed.", errors));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _ticketService.CheckInTicketAsync(request, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Validate a ticket without checking it in
        /// </summary>
        [HttpPost("validate")]
        [Authorize(Roles = "EventOrganizer,Admin")]
        public async Task<ActionResult<ApiResponse<ValidateTicketResponse>>> ValidateTicket([FromBody] ValidateTicketRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<ValidateTicketResponse>.ErrorResult("Validation failed.", errors));
            }

            var result = await _ticketService.ValidateTicketAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get ticket details by ticket code
        /// </summary>
        [HttpGet("code/{ticketCode}")]
        [Authorize(Roles = "EventOrganizer,Admin")]
        public async Task<ActionResult<ApiResponse<TicketCheckInDetails>>> GetTicketByCode(string ticketCode)
        {
            var result = await _ticketService.GetTicketDetailsByCodeAsync(ticketCode);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get all check-ins for an event
        /// </summary>
        [HttpGet("event/{eventId}/checkins")]
        [Authorize(Roles = "EventOrganizer,Admin")]
        public async Task<ActionResult<ApiResponse<List<TicketCheckInDetails>>>> GetEventCheckIns(int eventId)
        {
            var result = await _ticketService.GetEventCheckInsAsync(eventId);
            return Ok(result);
        }

        /// <summary>
        /// Undo a ticket check-in
        /// </summary>
        [HttpPost("{ticketId}/undo-checkin")]
        [Authorize(Roles = "EventOrganizer,Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> UndoCheckIn(int ticketId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _ticketService.UndoCheckInAsync(ticketId, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get all tickets for the current user
        /// </summary>
        [HttpGet("my-tickets")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<UserTicketDto>>>> GetMyTickets()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var result = await _ticketService.GetUserTicketsAsync(currentUserId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<List<UserTicketDto>>.ErrorResult($"Error retrieving tickets: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get QR code image for a specific ticket
        /// </summary>
        [HttpGet("{ticketId}/qr-image")]
        [Authorize]
        public async Task<ActionResult> GetTicketQRImage(int ticketId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var ticket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                    .FirstOrDefaultAsync(it => it.IssuedTicketID == ticketId && it.Registration.UserID == currentUserId);

                if (ticket == null)
                {
                    return NotFound("Ticket not found");
                }

                var qrImageBytes = await _qrCodeService.GenerateQRCodeImageAsync(ticket.QRCodeData);
                return File(qrImageBytes, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating QR image: {ex.Message}");
            }
        }

        /// <summary>
        /// Get QR code as Base64 string for a specific ticket
        /// </summary>
        [HttpGet("{ticketId}/qr-base64")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> GetTicketQRBase64(int ticketId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var ticket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                    .FirstOrDefaultAsync(it => it.IssuedTicketID == ticketId && it.Registration.UserID == currentUserId);

                if (ticket == null)
                {
                    return NotFound(ApiResponse<string>.ErrorResult("Ticket not found"));
                }

                var qrBase64 = await _qrCodeService.GenerateTicketQRCodeBase64Async(ticket.QRCodeData);
                return Ok(ApiResponse<string>.SuccessResult(qrBase64, "QR code generated"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<string>.ErrorResult($"Error generating QR code: {ex.Message}"));
            }
        }

        /// <summary>
        /// Send ticket via email to the attendee
        /// </summary>
        [HttpPost("{ticketId}/send-email")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> SendTicketEmail(int ticketId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var ticket = await _context.IssuedTickets
                    .Include(it => it.Registration)
                    .FirstOrDefaultAsync(it => it.IssuedTicketID == ticketId && it.Registration.UserID == currentUserId);

                if (ticket == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Ticket not found"));
                }

                var result = await _notificationService.SendTicketEmailAsync(ticketId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult($"Error sending ticket email: {ex.Message}"));
            }
        }

        /// <summary>
        /// DEMO: Decode QR code data to show what information is contained
        /// </summary>
        [HttpPost("decode-qr")]
        [Authorize(Roles = "EventOrganizer,Admin")]
        public async Task<ActionResult<ApiResponse<object>>> DecodeQRCode([FromBody] string qrData)
        {
            try
            {
                var ticketPayload = await _qrCodeService.ExtractTicketPayloadAsync(qrData);
                if (ticketPayload == null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid QR code data"));
                }

                var result = new
                {
                    RegistrationId = ticketPayload.RegistrationId,
                    TicketTypeId = ticketPayload.TicketTypeId,
                    UserEmail = ticketPayload.UserEmail,
                    IssuedAt = ticketPayload.IssuedAt,
                    ExpiresAt = ticketPayload.ExpiresAt,
                    IsValid = await _qrCodeService.ValidateTicketDataAsync(qrData),
                    DecodedAt = DateTime.UtcNow
                };

                return Ok(ApiResponse<object>.SuccessResult(result, "QR code decoded successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResult($"Error decoding QR code: {ex.Message}"));
            }
        }

        /// <summary>
        /// DEVELOPMENT ONLY: Regenerate QR codes for existing tickets
        /// </summary>
        [HttpPost("regenerate-qr/{registrationId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> RegenerateQRCode(int registrationId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.IssuedTickets)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

                if (registration == null)
                {
                    return NotFound(ApiResponse<bool>.ErrorResult("Registration not found"));
                }

                foreach (var ticket in registration.IssuedTickets)
                {
                    // Generate new secure QR code
                    var secureQRData = await _qrCodeService.GenerateSecureTicketDataAsync(
                        registration.RegisterID,
                        ticket.TicketTypeID,
                        registration.AttendeeEmail
                    );

                    ticket.QRCodeData = secureQRData;
                }

                await _context.SaveChangesAsync();
                return Ok(ApiResponse<bool>.SuccessResult(true, "QR codes regenerated"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResult($"Error: {ex.Message}"));
            }
        }

        /// <summary>
        /// Download ticket as PDF
        /// </summary>
        [HttpGet("{ticketId}/download-pdf")]
        [Authorize]
        public async Task<ActionResult> DownloadTicketPdf(int ticketId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                
                // Get ticket details with all necessary information
                var ticketData = await _context.IssuedTickets
                    .Include(it => it.Registration)
                        .ThenInclude(r => r.Event)
                            .ThenInclude(e => e.Venue)
                    .Include(it => it.TicketType)
                    .Where(it => it.IssuedTicketID == ticketId && it.Registration.UserID == currentUserId)
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
                    .FirstOrDefaultAsync();

                if (ticketData == null)
                {
                    return NotFound("Ticket not found");
                }

                // Generate PDF
                var pdfBytes = await _pdfService.GenerateTicketPdfAsync(ticketData);
                
                var fileName = $"Ticket-{ticketData.EventName.Replace(" ", "-")}-{ticketData.UniqueReferenceCode}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating PDF: {ex.Message}");
            }
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        #endregion
    }
}