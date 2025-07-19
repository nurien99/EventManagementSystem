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

        // ✅ UPDATE CONSTRUCTOR
        public TicketsController(
            ITicketService ticketService,
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            IEventAssistantService eventAssistantService)
        {
            _ticketService = ticketService;
            _context = context;
            _qrCodeService = qrCodeService;
            _eventAssistantService = eventAssistantService;
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

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        #endregion
    }
}