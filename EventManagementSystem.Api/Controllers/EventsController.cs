using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        /// <summary>
        /// Get all events with filtering and pagination
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResultDto<EventDto>>>> GetEvents([FromQuery] EventFilterDto filter)
        {
            var result = await _eventService.GetEventsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Get event by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<EventDto>>> GetEventById(int id)
        {
            var result = await _eventService.GetEventByIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Create a new event
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ApiResponse<EventDto>>> CreateEvent([FromBody] CreateEventDto createEventDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<EventDto>.ErrorResult("Validation failed.", errors));
            }

            // Validate business rules
            if (!createEventDto.IsValid(out var validationErrors))
            {
                return BadRequest(ApiResponse<EventDto>.ErrorResult("Validation failed.", validationErrors));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _eventService.CreateEventAsync(createEventDto, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetEventById), new { id = result.Data.EventID }, result);
        }

        /// <summary>
        /// Get current user's events
        /// </summary>
        [HttpGet("my-events")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<EventDto>>>> GetMyEvents()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventService.GetUserEventsAsync(currentUserId);
            return Ok(result);
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }

        #endregion

        /// <summary>
        /// Publish an event
        /// </summary>
        [HttpPut("{id}/publish")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<EventDto>>> PublishEvent(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventService.PublishEventAsync(id, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Cancel an event
        /// </summary>
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<EventDto>>> CancelEvent(int id, [FromBody] CancelEventDto cancelDto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventService.CancelEventAsync(id, currentUserId, cancelDto.Reason);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Update an event
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<EventDto>>> UpdateEvent(int id, [FromBody] CreateEventDto updateEventDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<EventDto>.ErrorResult("Validation failed.", errors));
            }

            var currentUserId = GetCurrentUserId();
            var result = await _eventService.UpdateEventAsync(id, updateEventDto, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete an event
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteEvent(int id)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventService.DeleteEventAsync(id, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get event by slug
        /// </summary>
        [HttpGet("slug/{slug}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<EventDto>>> GetEventBySlug(string slug)
        {
            var result = await _eventService.GetEventBySlugAsync(slug);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
    }
}