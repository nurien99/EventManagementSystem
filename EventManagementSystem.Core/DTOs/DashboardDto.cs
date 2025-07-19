namespace EventManagementSystem.Core.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public int TodayCheckIns { get; set; }
        public int TotalUsers { get; set; }
        public int TotalRevenue { get; set; }

        // Recent activity
        public List<RecentActivityDto> RecentActivities { get; set; } = new List<RecentActivityDto>();

        // Top events
        public List<EventStatsDto> TopEvents { get; set; } = new List<EventStatsDto>();
    }

    public class EventDashboardDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public EventStatus Status { get; set; }
        public string VenueName { get; set; } = string.Empty;

        // Statistics
        public int TotalRegistrations { get; set; }
        public int CheckedInCount { get; set; }
        public int PendingCount { get; set; }
        public decimal RegistrationRate { get; set; }
        public decimal CheckInRate { get; set; }

        // Recent check-ins
        public List<RecentCheckInDto> RecentCheckIns { get; set; } = new List<RecentCheckInDto>();

        // Registration trends (last 7 days)
        public List<DailyRegistrationDto> RegistrationTrend { get; set; } = new List<DailyRegistrationDto>();
    }

    public class RecentActivityDto
    {
        public string ActivityType { get; set; } = string.Empty; // "Registration", "CheckIn", "EventCreated"
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
    }

    public class EventStatsDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int RegistrationCount { get; set; }
        public int CheckInCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RecentCheckInDto
    {
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime CheckedInAt { get; set; }
        public string CheckedInBy { get; set; } = string.Empty;
        public string TicketType { get; set; } = string.Empty;
    }

    public class DailyRegistrationDto
    {
        public DateTime Date { get; set; }
        public int RegistrationCount { get; set; }
        public int CheckInCount { get; set; }
    }

    public class LiveEventStatsDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int TotalCapacity { get; set; }
        public int CurrentAttendance { get; set; }
        public int CheckedInToday { get; set; }
        public DateTime LastCheckIn { get; set; }
        public List<RecentCheckInDto> RecentCheckIns { get; set; } = new List<RecentCheckInDto>();
    }
}