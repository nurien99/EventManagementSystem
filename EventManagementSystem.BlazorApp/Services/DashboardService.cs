using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.BlazorApp.Services;

public class DashboardService
{
    private readonly ApiService _apiService;

    public DashboardService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<DashboardStatsDto?> GetDashboardStatsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<DashboardStatsDto>("api/dashboard/stats");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting dashboard stats: {ex.Message}");
            return null;
        }
    }

    public async Task<EventDashboardDto?> GetEventDashboardAsync(int eventId)
    {
        try
        {
            var response = await _apiService.GetAsync<EventDashboardDto>($"api/dashboard/event/{eventId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event dashboard: {ex.Message}");
            return null;
        }
    }

    public async Task<List<LiveEventStatsDto>> GetLiveEventStatsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<LiveEventStatsDto>>("api/dashboard/live-events");
            return response?.Data ?? new List<LiveEventStatsDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting live event stats: {ex.Message}");
            return new List<LiveEventStatsDto>();
        }
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int count = 10)
    {
        try
        {
            var response = await _apiService.GetAsync<List<RecentActivityDto>>($"api/dashboard/recent-activities?count={count}");
            return response?.Data ?? new List<RecentActivityDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting recent activities: {ex.Message}");
            return new List<RecentActivityDto>();
        }
    }

    public async Task<List<EventStatsDto>> GetTopEventsAsync(int count = 5)
    {
        try
        {
            var response = await _apiService.GetAsync<List<EventStatsDto>>($"api/dashboard/top-events?count={count}");
            return response?.Data ?? new List<EventStatsDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting top events: {ex.Message}");
            return new List<EventStatsDto>();
        }
    }

    public async Task<List<DailyRegistrationDto>> GetRegistrationTrendsAsync(int eventId, int days = 7)
    {
        try
        {
            var response = await _apiService.GetAsync<List<DailyRegistrationDto>>($"api/dashboard/event/{eventId}/trends?days={days}");
            return response?.Data ?? new List<DailyRegistrationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting registration trends: {ex.Message}");
            return new List<DailyRegistrationDto>();
        }
    }
}