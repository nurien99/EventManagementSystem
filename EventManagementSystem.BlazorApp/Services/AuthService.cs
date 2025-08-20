using System.Net.Http.Json;
using System.Text.Json;
using EventManagementSystem.Core.DTOs;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace EventManagementSystem.BlazorApp.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpClient = httpClientFactory.CreateClient("EventManagementAPI");
        _localStorage = localStorage;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task<ApiResponse<UserLoginResponseDto>?> LoginAsync(UserLoginDto loginDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/login", loginDto);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserLoginResponseDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Store token and user info
                    await _localStorage.SetItemAsync("authToken", apiResponse.Data.Token);
                    await _localStorage.SetItemAsync("userInfo", apiResponse.Data.User);
                    await _localStorage.SetItemAsync("tokenExpires", apiResponse.Data.ExpiresAt);

                    // Notify authentication state has changed
                    await ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(apiResponse.Data.User);

                    return apiResponse;
                }
            }

            var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<UserLoginResponseDto>>();
            return errorContent ?? new ApiResponse<UserLoginResponseDto>
            {
                Success = false,
                Message = "Login failed"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserLoginResponseDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<UserDto>?> RegisterAsync(UserRegisterDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/register", registerDto);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            }

            var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            return errorContent ?? new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Registration failed"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            // Clear local storage
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("userInfo");
            await _localStorage.RemoveItemAsync("tokenExpires");

            // Notify authentication state has changed
            await ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<UserDto>("userInfo");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var expiry = await _localStorage.GetItemAsync<DateTime?>("tokenExpires");

            return !string.IsNullOrEmpty(token) &&
                   expiry.HasValue &&
                   expiry.Value > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ApiResponse<bool>?> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/forgot-password", forgotPasswordDto);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<bool>?> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/reset-password", resetPasswordDto);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<bool>?> VerifyEmailAsync(string email, string token)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/users/verify-email?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}", null);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<bool>?> ResendVerificationEmailAsync(string email)
    {
        try
        {
            var requestData = new { Email = email };
            var response = await _httpClient.PostAsJsonAsync("api/users/resend-verification", requestData);
            return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<ApiResponse<UserDto>?> UpgradeToOrganizerAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("api/users/upgrade-to-organizer", null);

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    // Update stored user info with new role
                    await _localStorage.SetItemAsync("userInfo", apiResponse.Data);

                    // Update authentication state with new role
                    await ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(apiResponse.Data);

                    return apiResponse;
                }
            }

            var errorContent = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
            return errorContent ?? new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Role upgrade failed"
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task UpdateUserInfoAsync(UserDto user)
    {
        try
        {
            // Update stored user info
            await _localStorage.SetItemAsync("userInfo", user);

            // Update authentication state
            await ((CustomAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user info: {ex.Message}");
        }
    }
}