
using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(int? userId = null);
        Task<ApiResponse<EventDashboardDto>> GetEventDashboardAsync(int eventId);
        Task<ApiResponse<List<LiveEventStatsDto>>> GetLiveEventStatsAsync();
        Task<ApiResponse<List<RecentActivityDto>>> GetRecentActivitiesAsync(int count = 10);
        Task<ApiResponse<List<EventStatsDto>>> GetTopEventsAsync(int count = 5);
        Task<ApiResponse<List<DailyRegistrationDto>>> GetRegistrationTrendsAsync(int eventId, int days = 7);
    }
}