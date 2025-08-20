namespace EventManagementSystem.Core.DTOs.Email
{
    public abstract class BaseEmailModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SiteName { get; set; } = string.Empty;
        public string SiteUrl { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class UserWelcomeModel : BaseEmailModel
    {
        public string EmailVerificationUrl { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
    }

    public class PasswordResetModel : BaseEmailModel
    {
        public string ResetUrl { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public class RegistrationConfirmationModel : BaseEmailModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public DateTime EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string QRCodeBase64 { get; set; } = string.Empty;
        public List<TicketTypeInfo> TicketTypes { get; set; } = new List<TicketTypeInfo>();
        public string RegistrationId { get; set; } = string.Empty;
        public string CancellationUrl { get; set; } = string.Empty;
    }

    public class EventReminderModel : BaseEmailModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime EventStartDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string EventUrl { get; set; } = string.Empty;
        public string HoursUntilEvent { get; set; } = string.Empty;
        public string SpecialInstructions { get; set; } = string.Empty;
    }

    public class EventCancellationModel : BaseEmailModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public DateTime OriginalEventDate { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
        public string RefundInformation { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
    }

    public class TicketDeliveryModel : BaseEmailModel
    {
        public string EventTitle { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public DateTime EventStartDate { get; set; }
        public DateTime? EventEndDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string TicketNumber { get; set; } = string.Empty;
        public string TicketTypeName { get; set; } = string.Empty;
        public decimal TicketPrice { get; set; }
        public string AttendeeName { get; set; } = string.Empty;
        public string QRCodeBase64 { get; set; } = string.Empty;
        public string EventUrl { get; set; } = string.Empty;
        public string CheckInInstructions { get; set; } = string.Empty;
    }

    public class TicketTypeInfo
    {
        public string TypeName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}