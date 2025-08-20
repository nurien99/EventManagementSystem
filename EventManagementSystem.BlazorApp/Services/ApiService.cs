using System.Net.Http.Json;
using System.Text.Json;
using EventManagementSystem.Core.DTOs;
using Blazored.LocalStorage;
using Microsoft.JSInterop;

namespace EventManagementSystem.BlazorApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly IJSRuntime _jsRuntime;

    public ApiService(IHttpClientFactory httpClientFactory, ILocalStorageService localStorage, IJSRuntime jsRuntime)
    {
        _httpClient = httpClientFactory.CreateClient("EventManagementAPI");
        _localStorage = localStorage;
        _jsRuntime = jsRuntime;
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        await SetAuthHeaderAsync();

        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<T>>(endpoint);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Error: {ex.Message}");
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<T>?> PostAsync<T>(string endpoint, object data)
    {
        await SetAuthHeaderAsync();

        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new ApiResponse<T> { Success = false, Message = $"Request failed: {errorContent}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<T>?> PutAsync<T>(string endpoint, object data)
    {
        await SetAuthHeaderAsync();

        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApiResponse<T>>();
            }

            return new ApiResponse<T> { Success = false, Message = "Request failed" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        await SetAuthHeaderAsync();

        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task SetAuthHeaderAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch
        {
            // LocalStorage might not be available during prerendering
        }
    }

    public HttpClient GetHttpClient()
    {
        return _httpClient;
    }

    public async Task<Stream?> GetStreamAsync(string endpoint)
    {
        await SetAuthHeaderAsync();

        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API Stream Error: {ex.Message}");
            return null;
        }
    }

    public async Task TriggerFileDownloadAsync(string base64Content, string fileName, string contentType)
    {
        await _jsRuntime.InvokeVoidAsync("downloadFileFromBase64", base64Content, fileName, contentType);
    }
}