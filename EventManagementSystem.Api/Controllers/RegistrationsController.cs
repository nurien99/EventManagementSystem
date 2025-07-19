using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationsController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;

        public RegistrationsController(IRegistrationService registrationService)
        {
            _registrationService = registrationService;
        }

        /// <summary>
        /// Register for an event
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<RegistrationDto>>> RegisterForEvent([FromBody] CreateRegistrationDto createRegistrationDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<RegistrationDto>.ErrorResult("Validation failed.", errors));
            }

            // If user is authenticated and UserID is not provided, use the current user's ID
            if (User.Identity.IsAuthenticated && !createRegistrationDto.UserID.HasValue)
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId > 0)
                {
                    createRegistrationDto.UserID = currentUserId;
                }
            }

            var result = await _registrationService.RegisterForEventAsync(createRegistrationDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetRegistrationById), new { id = result.Data.RegisterID }, result);
        }

        /// <summary>
        /// Get current user's registrations
        /// </summary>
        [HttpGet("my-registrations")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<RegistrationDto>>>> GetMyRegistrations()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _registrationService.GetUserRegistrationsAsync(currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Get registration by ID (for current user)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RegistrationDto>>> GetRegistrationById(int id)
        {
            var currentUserId = GetCurrentUserId();

            // Get user's registrations and find the specific one
            var userRegistrations = await _registrationService.GetUserRegistrationsAsync(currentUserId);
            if (!userRegistrations.Success)
            {
                return BadRequest(userRegistrations);
            }

            var registration = userRegistrations.Data.FirstOrDefault(r => r.RegisterID == id);
            if (registration == null)
            {
                return NotFound(ApiResponse<RegistrationDto>.ErrorResult("Registration not found."));
            }

            return Ok(ApiResponse<RegistrationDto>.SuccessResult(registration));
        }

        /// <summary>
        /// Cancel a registration
        /// </summary>
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> CancelRegistration(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _registrationService.CancelRegistrationAsync(id, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get registrations for an event (for event organizers)
        /// </summary>
        [HttpGet("event/{eventId}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<RegistrationDto>>>> GetEventRegistrations(int eventId)
        {
            // Note: In a production system, you'd want to verify that the current user
            // is the organizer of this event or has admin permissions
            var result = await _registrationService.GetEventRegistrationsAsync(eventId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Register for an event as a guest (no authentication required)
        /// </summary>
        [HttpPost("guest")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RegistrationDto>>> RegisterAsGuest([FromBody] CreateRegistrationDto createRegistrationDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<RegistrationDto>.ErrorResult("Validation failed.", errors));
            }

            // Ensure UserID is null for guest registration
            createRegistrationDto.UserID = null;

            var result = await _registrationService.RegisterForEventAsync(createRegistrationDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetRegistrationById), new { id = result.Data.RegisterID }, result);
        }

        /// <summary>
        /// Get registrations by email (for guests)
        /// </summary>
        [HttpGet("lookup/{email}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<RegistrationDto>>>> GetRegistrationsByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ApiResponse<List<RegistrationDto>>.ErrorResult("Email is required."));
            }

            var result = await _registrationService.GetRegistrationsByEmailAsync(email);
            return Ok(result);
        }

        /// <summary>
        /// Get specific registration by email and registration ID (for guests)
        /// </summary>
        [HttpGet("lookup/{email}/{registrationId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RegistrationDto>>> GetRegistrationByEmailAndId(string email, int registrationId)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ApiResponse<RegistrationDto>.ErrorResult("Email is required."));
            }

            var result = await _registrationService.GetRegistrationByEmailAndIdAsync(email, registrationId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Cancel registration by email and registration ID (for guests)
        /// </summary>
        [HttpPut("lookup/{email}/{registrationId}/cancel")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> CancelRegistrationByEmail(string email, int registrationId)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(ApiResponse<bool>.ErrorResult("Email is required."));
            }

            var result = await _registrationService.CancelRegistrationByEmailAsync(email, registrationId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
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