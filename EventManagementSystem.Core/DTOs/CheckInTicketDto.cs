using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class CheckInTicketRequest
    {
        [Required]
        public string QRCodeData { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    public class CheckInTicketResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TicketCheckInDetails? TicketDetails { get; set; }
    }

    public class TicketCheckInDetails
    {
        public int IssuedTicketID { get; set; }
        public string UniqueReferenceCode { get; set; } = string.Empty;
        public string AttendeeName { get; set; } = string.Empty;
        public string AttendeeEmail { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string TicketTypeName { get; set; } = string.Empty;
        public DateTime? CheckedInAt { get; set; }
        public string CheckedInByUser { get; set; } = string.Empty;
        public TicketStatus Status { get; set; }
    }

    public class ValidateTicketRequest
    {
        [Required]
        public string QRCodeData { get; set; } = string.Empty;
    }

    public class ValidateTicketResponse
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public TicketCheckInDetails? TicketDetails { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}