using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core
{
    public enum EventStatus
    {
        Draft = 0,
        Published = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum UserRole
    {
        Attendee = 0,
        EventOrganizer = 1,
        Admin = 2
    }

    public enum RegistrationStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2,
        CheckedIn = 3,
        NoShow = 4
    }

    public enum TicketStatus
    {
        Valid = 0,
        Used = 1,
        Cancelled = 2,
        Expired = 3
    }

    public enum EmailType
    {
        UserRegistration = 0,
        EventRegistrationConfirmation = 1,
        EventReminder = 2,
        EventCancellation = 3,
        EventUpdate = 4,
        PasswordReset = 5,
        EmailVerification = 6,
        TicketQRCode = 7,
        WelcomeEmail = 8,
        SystemNotification = 9
    }

    public enum EmailStatus
    {
        Pending = 0,
        Sent = 1,
        Failed = 2,
        Cancelled = 3
    }

    public enum AssistantRole
    {
        CheckInOnly = 0,      // Can only check-in tickets
        ViewAttendees = 1,    // Can view attendee lists
        FullAssistant = 2     // Can do everything except delete event
    }
}