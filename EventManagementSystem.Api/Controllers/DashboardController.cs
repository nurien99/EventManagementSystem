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
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        /// <summary>
        /// Get overall dashboard statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetDashboardStats()
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                // Admins see all data, others see only their data
                var userId = userRole == "Admin" ? (int?)null : currentUserId;

                var result = await _dashboardService.GetDashboardStatsAsync(userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ApiResponse<DashboardStatsDto>.ErrorResult(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<DashboardStatsDto>.ErrorResult("Error retrieving dashboard statistics", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get dashboard for a specific event
        /// </summary>
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<ApiResponse<EventDashboardDto>>> GetEventDashboard(int eventId)
        {
            var result = await _dashboardService.GetEventDashboardAsync(eventId);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get live statistics for events happening today/tomorrow
        /// </summary>
        [HttpGet("live-events")]
        public async Task<ActionResult<ApiResponse<List<LiveEventStatsDto>>>> GetLiveEventStats()
        {
            var result = await _dashboardService.GetLiveEventStatsAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get recent activities across the platform
        /// </summary>
        [HttpGet("recent-activities")]
        public async Task<ActionResult<ApiResponse<List<RecentActivityDto>>>> GetRecentActivities([FromQuery] int count = 10)
        {
            var result = await _dashboardService.GetRecentActivitiesAsync(count);
            return Ok(result);
        }

        /// <summary>
        /// Get top performing events
        /// </summary>
        [HttpGet("top-events")]
        public async Task<ActionResult<ApiResponse<List<EventStatsDto>>>> GetTopEvents([FromQuery] int count = 5)
        {
            var result = await _dashboardService.GetTopEventsAsync(count);
            return Ok(result);
        }

        /// <summary>
        /// Get registration trends for an event
        /// </summary>
        [HttpGet("event/{eventId}/trends")]
        public async Task<ActionResult<ApiResponse<List<DailyRegistrationDto>>>> GetRegistrationTrends(int eventId, [FromQuery] int days = 7)
        {
            var result = await _dashboardService.GetRegistrationTrendsAsync(eventId, days);
            return Ok(result);
        }

        #region Helper Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim?.Value == null)
            {
                throw new UnauthorizedAccessException("User ID claim not found in token");
            }
            
            if (!int.TryParse(userIdClaim.Value, out int userId) || userId <= 0)
            {
                throw new UnauthorizedAccessException("Invalid user ID in token");
            }
            
            return userId;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Attendee";
        }

        #endregion
    }
}