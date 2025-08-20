namespace EventManagementSystem.Core.DTOs
{
    public class AdminUserDto
    {
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsEmailVerified { get; set; }
        public DateTime DateRegistered { get; set; }
        public int? EventsCreated { get; set; }
        public int? EventsAttended { get; set; }
    }

    public class AdminUserDetailDto : AdminUserDto
    {
        public DateTime? LastLoginDate { get; set; }
        public new List<AdminEventSummaryDto> EventsCreated { get; set; } = new();
        public List<AdminRegistrationDto> Registrations { get; set; } = new();
    }

    public class AdminEventSummaryDto
    {
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public EventStatus Status { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class AdminRegistrationDto
    {
        public int RegistrationID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public RegistrationStatus Status { get; set; }
    }

    public class UpdateUserRoleDto
    {
        public UserRole NewRole { get; set; }
    }

    public class AdminStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalEvents { get; set; }
        public int TotalRegistrations { get; set; }
        public decimal TotalRevenue { get; set; }

        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public Dictionary<string, int> EventsByStatus { get; set; } = new();
        public List<AdminActivityDto> RecentActivity { get; set; } = new();
    }

    public class AdminActivityDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public int? EventId { get; set; }
        public string? EventName { get; set; }
    }

    public class AdminEventDto
    {
        public int EventID { get; set; }
        public string Name { get; set; } = string.Empty;
        public EventStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxCapacity { get; set; }
        public int AvailableTickets { get; set; }
        public string OrganizerName { get; set; } = string.Empty;
        public string OrganizerEmail { get; set; } = string.Empty;
        public string VenueName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalRegistrations { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class UpdateEventStatusDto
    {
        public EventStatus NewStatus { get; set; }
    }
}