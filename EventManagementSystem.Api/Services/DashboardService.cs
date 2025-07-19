// File: EventManagementSystem.Api/Services/DashboardService.cs
using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ApplicationDbContext context, ILogger<DashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(int? userId = null)
        {
            try
            {
                var today = DateTime.Today;

                // Build base queries
                var eventsQuery = _context.Events.AsQueryable();
                var registrationsQuery = _context.Registrations.AsQueryable();

                // Filter by user if specified (for event organizers)
                if (userId.HasValue)
                {
                    eventsQuery = eventsQuery.Where(e => e.UserID == userId.Value);
                    registrationsQuery = registrationsQuery.Where(r => r.Event.UserID == userId.Value);
                }

                var stats = new DashboardStatsDto
                {
                    TotalEvents = await eventsQuery.CountAsync(),
                    ActiveEvents = await eventsQuery.CountAsync(e => e.Status == EventStatus.Published),
                    TotalRegistrations = await registrationsQuery.CountAsync(r => r.Status == RegistrationStatus.Confirmed),
                    TodayCheckIns = await _context.IssuedTickets
                        .Where(it => it.CheckedInAt.HasValue && it.CheckedInAt.Value.Date == today)
                        .CountAsync(),
                    TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                    TotalRevenue = (int)await _context.TicketTypes
                        .Where(tt => tt.SoldQuantity > 0)
                        .SumAsync(tt => tt.Price * tt.SoldQuantity)
                };

                // Get recent activities
                stats.RecentActivities = await GetRecentActivitiesInternalAsync(5);

                // Get top events
                stats.TopEvents = await GetTopEventsInternalAsync(5, userId);

                return ApiResponse<DashboardStatsDto>.SuccessResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return ApiResponse<DashboardStatsDto>.ErrorResult("Error retrieving dashboard statistics", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventDashboardDto>> GetEventDashboardAsync(int eventId)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Venue)
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    return ApiResponse<EventDashboardDto>.ErrorResult("Event not found");
                }

                var totalRegistrations = eventEntity.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed);
                var checkedInCount = await _context.IssuedTickets
                    .Where(it => it.Registration.EventID == eventId && it.CheckedInAt.HasValue)
                    .CountAsync();

                var dashboard = new EventDashboardDto
                {
                    EventID = eventEntity.EventID,
                    EventName = eventEntity.EventName,
                    StartDate = eventEntity.StartDate,
                    Status = eventEntity.Status,
                    VenueName = eventEntity.Venue?.VenueName ?? "Unknown",
                    TotalRegistrations = totalRegistrations,
                    CheckedInCount = checkedInCount,
                    PendingCount = totalRegistrations - checkedInCount,
                    RegistrationRate = eventEntity.MaxCapacity.HasValue ?
                        (decimal)totalRegistrations / eventEntity.MaxCapacity.Value * 100 : 0,
                    CheckInRate = totalRegistrations > 0 ?
                        (decimal)checkedInCount / totalRegistrations * 100 : 0
                };

                // Get recent check-ins
                dashboard.RecentCheckIns = await GetRecentCheckInsForEventAsync(eventId, 10);

                // Get registration trends
                dashboard.RegistrationTrend = await GetRegistrationTrendsInternalAsync(eventId, 7);

                return ApiResponse<EventDashboardDto>.SuccessResult(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event dashboard for event {EventId}", eventId);
                return ApiResponse<EventDashboardDto>.ErrorResult("Error retrieving event dashboard", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<LiveEventStatsDto>>> GetLiveEventStatsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                // Get events happening today or tomorrow
                var liveEvents = await _context.Events
                    .Where(e => e.StartDate.Date >= today && e.StartDate.Date <= tomorrow)
                    .Where(e => e.Status == EventStatus.Published)
                    .Select(e => new LiveEventStatsDto
                    {
                        EventID = e.EventID,
                        EventName = e.EventName,
                        TotalCapacity = e.MaxCapacity ?? 0,
                        CurrentAttendance = e.Registrations.Count(r => r.Status == RegistrationStatus.CheckedIn),
                        CheckedInToday = _context.IssuedTickets
                            .Count(it => it.Registration.EventID == e.EventID &&
                                        it.CheckedInAt.HasValue &&
                                        it.CheckedInAt.Value.Date == today),
                        LastCheckIn = _context.IssuedTickets
                            .Where(it => it.Registration.EventID == e.EventID && it.CheckedInAt.HasValue)
                            .Max(it => it.CheckedInAt) ?? DateTime.MinValue
                    })
                    .ToListAsync();

                // Get recent check-ins for each event
                foreach (var eventStats in liveEvents)
                {
                    eventStats.RecentCheckIns = await GetRecentCheckInsForEventAsync(eventStats.EventID, 5);
                }

                return ApiResponse<List<LiveEventStatsDto>>.SuccessResult(liveEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live event stats");
                return ApiResponse<List<LiveEventStatsDto>>.ErrorResult("Error retrieving live event statistics", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<RecentActivityDto>>> GetRecentActivitiesAsync(int count = 10)
        {
            var activities = await GetRecentActivitiesInternalAsync(count);
            return ApiResponse<List<RecentActivityDto>>.SuccessResult(activities);
        }

        public async Task<ApiResponse<List<EventStatsDto>>> GetTopEventsAsync(int count = 5)
        {
            var topEvents = await GetTopEventsInternalAsync(count);
            return ApiResponse<List<EventStatsDto>>.SuccessResult(topEvents);
        }

        public async Task<ApiResponse<List<DailyRegistrationDto>>> GetRegistrationTrendsAsync(int eventId, int days = 7)
        {
            var trends = await GetRegistrationTrendsInternalAsync(eventId, days);
            return ApiResponse<List<DailyRegistrationDto>>.SuccessResult(trends);
        }

        #region Private Helper Methods

        private async Task<List<RecentActivityDto>> GetRecentActivitiesInternalAsync(int count)
        {
            var activities = new List<RecentActivityDto>();

            // Recent registrations
            var recentRegistrations = await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .Where(r => r.RegisteredAt.HasValue)
                .OrderByDescending(r => r.RegisteredAt)
                .Take(count / 2)
                .Select(r => new RecentActivityDto
                {
                    ActivityType = "Registration",
                    Description = $"New registration for {r.Event.EventName}",
                    Timestamp = r.RegisteredAt.Value,
                    UserName = r.AttendeeName,
                    EventName = r.Event.EventName
                })
                .ToListAsync();

            // Recent check-ins
            var recentCheckIns = await _context.IssuedTickets
                .Include(it => it.Registration)
                    .ThenInclude(r => r.Event)
                .Include(it => it.CheckedInByUser)
                .Where(it => it.CheckedInAt.HasValue)
                .OrderByDescending(it => it.CheckedInAt)
                .Take(count / 2)
                .Select(it => new RecentActivityDto
                {
                    ActivityType = "CheckIn",
                    Description = $"Checked in to {it.Registration.Event.EventName}",
                    Timestamp = it.CheckedInAt.Value,
                    UserName = it.AttendeeName,
                    EventName = it.Registration.Event.EventName
                })
                .ToListAsync();

            activities.AddRange(recentRegistrations);
            activities.AddRange(recentCheckIns);

            return activities.OrderByDescending(a => a.Timestamp).Take(count).ToList();
        }

        private async Task<List<EventStatsDto>> GetTopEventsInternalAsync(int count, int? userId = null)
        {
            var query = _context.Events.AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(e => e.UserID == userId.Value);
            }

            return await query
                .Select(e => new EventStatsDto
                {
                    EventID = e.EventID,
                    EventName = e.EventName,
                    RegistrationCount = e.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed),
                    CheckInCount = _context.IssuedTickets.Count(it => it.Registration.EventID == e.EventID && it.CheckedInAt.HasValue),
                    Revenue = e.TicketTypes.Sum(tt => tt.Price * tt.SoldQuantity)
                })
                .OrderByDescending(e => e.RegistrationCount)
                .Take(count)
                .ToListAsync();
        }

        private async Task<List<RecentCheckInDto>> GetRecentCheckInsForEventAsync(int eventId, int count)
        {
            return await _context.IssuedTickets
                .Include(it => it.TicketType)
                .Include(it => it.CheckedInByUser)
                .Where(it => it.Registration.EventID == eventId && it.CheckedInAt.HasValue)
                .OrderByDescending(it => it.CheckedInAt)
                .Take(count)
                .Select(it => new RecentCheckInDto
                {
                    AttendeeName = it.AttendeeName,
                    AttendeeEmail = it.AttendeeEmail,
                    CheckedInAt = it.CheckedInAt.Value,
                    CheckedInBy = it.CheckedInByUser.Name ?? "System",
                    TicketType = it.TicketType.TypeName
                })
                .ToListAsync();
        }

        private async Task<List<DailyRegistrationDto>> GetRegistrationTrendsInternalAsync(int eventId, int days)
        {
            var startDate = DateTime.Today.AddDays(-days);
            var endDate = DateTime.Today;

            var registrations = await _context.Registrations
                .Where(r => r.EventID == eventId &&
                           r.RegisteredAt.HasValue &&
                           r.RegisteredAt.Value.Date >= startDate)
                .GroupBy(r => r.RegisteredAt.Value.Date)
                .Select(g => new DailyRegistrationDto
                {
                    Date = g.Key,
                    RegistrationCount = g.Count(),
                    CheckInCount = _context.IssuedTickets
                        .Count(it => it.Registration.EventID == eventId &&
                                    it.CheckedInAt.HasValue &&
                                    it.CheckedInAt.Value.Date == g.Key)
                })
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Fill in missing dates with zero counts
            var allDates = Enumerable.Range(0, days + 1)
                .Select(i => startDate.AddDays(i))
                .ToList();

            var result = allDates.Select(date =>
                registrations.FirstOrDefault(r => r.Date == date) ??
                new DailyRegistrationDto { Date = date, RegistrationCount = 0, CheckInCount = 0 })
                .ToList();

            return result;
        }

        #endregion
    }
}