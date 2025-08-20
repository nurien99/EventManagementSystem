using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Blazored.LocalStorage;
using EventManagementSystem.Core.DTOs;
using System.Text.Json;

namespace EventManagementSystem.BlazorApp.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

    public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            var userInfo = await _localStorage.GetItemAsync<UserDto>("userInfo");
            var expiry = await _localStorage.GetItemAsync<DateTime?>("tokenExpires");

            if (string.IsNullOrEmpty(token) ||
                userInfo == null ||
                !expiry.HasValue ||
                expiry.Value <= DateTime.UtcNow)
            {
                return new AuthenticationState(_anonymous);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userInfo.UserID.ToString()),
                new Claim(ClaimTypes.Name, userInfo.Name),
                new Claim(ClaimTypes.Email, userInfo.Email),
                new Claim(ClaimTypes.Role, userInfo.Role.ToString()),
                new Claim("userId", userInfo.UserID.ToString()),
                new Claim("isEmailVerified", userInfo.IsEmailVerified.ToString())
            };

            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "JWT"));
            return new AuthenticationState(authenticatedUser);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(UserDto user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("userId", user.UserID.ToString()),
            new Claim("isEmailVerified", user.IsEmailVerified.ToString())
        };

        var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "JWT"));
        var authState = Task.FromResult(new AuthenticationState(authenticatedUser));

        NotifyAuthenticationStateChanged(authState);
    }

    public async Task MarkUserAsLoggedOut()
    {
        var authState = Task.FromResult(new AuthenticationState(_anonymous));
        NotifyAuthenticationStateChanged(authState);
    }
}