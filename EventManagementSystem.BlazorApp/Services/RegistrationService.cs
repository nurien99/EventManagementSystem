using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.BlazorApp.Services;

public class RegistrationService
{
    private readonly ApiService _apiService;

    public RegistrationService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<ApiResponse<RegistrationDto>?> RegisterForEventAsync(CreateRegistrationDto registrationDto)
    {
        try
        {
            return await _apiService.PostAsync<RegistrationDto>("api/registrations", registrationDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering for event: {ex.Message}");
            return ApiResponse<RegistrationDto>.ErrorResult($"Error registering for event: {ex.Message}");
        }
    }

    public async Task<ApiResponse<RegistrationDto>?> RegisterAsGuestAsync(CreateRegistrationDto registrationDto)
    {
        try
        {
            return await _apiService.PostAsync<RegistrationDto>("api/registrations/guest", registrationDto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering as guest: {ex.Message}");
            return ApiResponse<RegistrationDto>.ErrorResult($"Error registering as guest: {ex.Message}");
        }
    }

    public async Task<List<RegistrationDto>> GetMyRegistrationsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<RegistrationDto>>("api/registrations/my-registrations");
            return response?.Data ?? new List<RegistrationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting my registrations: {ex.Message}");
            return new List<RegistrationDto>();
        }
    }

    public async Task<RegistrationDto?> GetRegistrationByIdAsync(int registrationId)
    {
        try
        {
            var response = await _apiService.GetAsync<RegistrationDto>($"api/registrations/{registrationId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting registration {registrationId}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> CancelRegistrationAsync(int registrationId)
    {
        try
        {
            var response = await _apiService.PutAsync<bool>($"api/registrations/{registrationId}/cancel", null);
            return response?.Success ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cancelling registration {registrationId}: {ex.Message}");
            return false;
        }
    }

    public async Task<List<RegistrationDto>> GetRegistrationsByEmailAsync(string email)
    {
        try
        {
            var response = await _apiService.GetAsync<List<RegistrationDto>>($"api/registrations/lookup/{Uri.EscapeDataString(email)}");
            return response?.Data ?? new List<RegistrationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting registrations for email {email}: {ex.Message}");
            return new List<RegistrationDto>();
        }
    }

    public async Task<bool> CancelRegistrationByEmailAsync(string email, int registrationId)
    {
        try
        {
            var response = await _apiService.PutAsync<bool>($"api/registrations/lookup/{Uri.EscapeDataString(email)}/{registrationId}/cancel", null);
            return response?.Success ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cancelling registration for email {email}: {ex.Message}");
            return false;
        }
    }
}