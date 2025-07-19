using EventManagementSystem.Core.DTOs.Email;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IEmailTemplateService
    {
        Task<string> RenderTemplateAsync<T>(string templateName, T model) where T : BaseEmailModel;
        Task<bool> ValidateTemplateAsync(string templateName);
        List<string> GetAvailableTemplates();
    }
}