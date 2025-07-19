using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EventAssistantsController : ControllerBase
    {
        private readonly IEventAssistantService _eventAssistantService;

        public EventAssistantsController(IEventAssistantService eventAssistantService)
        {
            _eventAssistantService = eventAssistantService;
        }

        /// <summary>
        /// Assign an assistant to an event (Event organizer only)
        /// </summary>
        [HttpPost("assign")]
        public async Task<ActionResult<ApiResponse<EventAssistantDto>>> AssignAssistant([FromBody] AssignAssistantDto assignDto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventAssistantService.AssignAssistantAsync(assignDto, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get all assistants for an event
        /// </summary>
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<ApiResponse<List<EventAssistantDto>>>> GetEventAssistants(int eventId)
        {
            var result = await _eventAssistantService.GetEventAssistantsAsync(eventId);
            return Ok(result);
        }

        /// <summary>
        /// Update assistant role
        /// </summary>
        [HttpPut("{assistantId}/role")]
        public async Task<ActionResult<ApiResponse<EventAssistantDto>>> UpdateAssistantRole(int assistantId, [FromBody] UpdateAssistantRoleDto updateDto)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventAssistantService.UpdateAssistantRoleAsync(assistantId, updateDto, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Remove assistant from event
        /// </summary>
        [HttpDelete("{assistantId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveAssistant(int assistantId)
        {
            var currentUserId = GetCurrentUserId();
            var result = await _eventAssistantService.RemoveAssistantAsync(assistantId, currentUserId);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim?.Value ?? "0");
        }
    }
}