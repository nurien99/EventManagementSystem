using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IRegistrationService
    {
        Task<ApiResponse<RegistrationDto>> RegisterForEventAsync(CreateRegistrationDto registrationDto);
        Task<ApiResponse<List<RegistrationDto>>> GetUserRegistrationsAsync(int userId);
        Task<ApiResponse<List<RegistrationDto>>> GetEventRegistrationsAsync(int eventId);
        Task<ApiResponse<bool>> CancelRegistrationAsync(int registrationId, int userId);

        // New methods for guest email-based lookup
        Task<ApiResponse<List<RegistrationDto>>> GetRegistrationsByEmailAsync(string email);
        Task<ApiResponse<RegistrationDto>> GetRegistrationByEmailAndIdAsync(string email, int registrationId);
        Task<ApiResponse<bool>> CancelRegistrationByEmailAsync(string email, int registrationId);
    }
}