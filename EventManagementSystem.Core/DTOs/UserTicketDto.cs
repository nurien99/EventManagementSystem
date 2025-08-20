using EventManagementSystem.Core;

namespace EventManagementSystem.Core.DTOs
{
    public class UserTicketDto
    {
        public int IssuedTicketID { get; set; }
        public string UniqueReferenceCode { get; set; } = string.Empty;
        public string QRCodeData { get; set; } = string.Empty;
        public string TicketTypeName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public DateTime? CheckedInAt { get; set; }
        public TicketStatus Status { get; set; }
        public DateTime IssuedAt { get; set; }
        
        // Event Details
        public int EventID { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventSlug { get; set; } = string.Empty;
        public DateTime EventStartDate { get; set; }
        public DateTime EventEndDate { get; set; }
        public EventStatus EventStatus { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string? EventImageUrl { get; set; }
        
        // Registration Details
        public int RegistrationID { get; set; }
        public DateTime RegisteredAt { get; set; }
        public RegistrationStatus RegistrationStatus { get; set; }
        
        // Additional Info
        public bool CanCheckIn => Status == TicketStatus.Valid && 
                                  EventStatus == EventStatus.Published && 
                                  DateTime.Now >= EventStartDate.AddHours(-2) && 
                                  DateTime.Now <= EventEndDate;
        
        public bool IsEventActive => EventStartDate <= DateTime.Now && EventEndDate >= DateTime.Now;
        public bool IsEventUpcoming => EventStartDate > DateTime.Now;
        public bool IsEventPast => EventEndDate < DateTime.Now;
    }
}