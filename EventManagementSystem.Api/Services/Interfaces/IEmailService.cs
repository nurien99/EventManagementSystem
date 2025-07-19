using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core.DTOs.Email;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IEmailService
    {
        Task<ApiResponse<bool>> SendEmailAsync(EmailMessageDto emailMessage);
        Task<ApiResponse<bool>> QueueEmailAsync(EmailMessageDto emailMessage);
        Task<ApiResponse<bool>> SendTemplateEmailAsync<T>(string toEmail, string subject, string templateName, T model) where T : BaseEmailModel;
        Task<ApiResponse<bool>> ProcessPendingEmailsAsync();
        Task<ApiResponse<List<EmailOutbox>>> GetEmailHistoryAsync(int? relatedEntityId = null, Core.EmailType? type = null); // ✅ Fully qualified
        Task<ApiResponse<EmailOutbox>> GetEmailByIdAsync(int emailId);
    }
}