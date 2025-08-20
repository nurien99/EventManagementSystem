using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface INotificationService
    {
        // User account notifications
        Task<ApiResponse<bool>> SendWelcomeEmailAsync(int userId);
        Task<ApiResponse<bool>> SendEmailVerificationAsync(int userId);

        // Event-related notifications
        Task<ApiResponse<bool>> SendRegistrationConfirmationAsync(int registrationId);
        Task<ApiResponse<bool>> SendEventReminderAsync(int eventId, int hoursBeforeEvent);
        Task<ApiResponse<bool>> SendEventCancellationAsync(int eventId, string reason);
        Task<ApiResponse<bool>> SendEventUpdateAsync(int eventId, string updateMessage);
        Task<ApiResponse<bool>> SendTicketEmailAsync(int ticketId);

        // Bulk notifications
        Task<ApiResponse<bool>> SendBulkEventRemindersAsync(int eventId);
        Task<ApiResponse<bool>> SendBulkEventCancellationAsync(int eventId, string reason);
    }
}