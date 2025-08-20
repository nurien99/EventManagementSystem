using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core;
using Microsoft.AspNetCore.Components.Forms;

namespace EventManagementSystem.BlazorApp.Services;

public class EventService
{
    private readonly ApiService _apiService;

    public EventService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<EventDto>> GetEventsAsync(EventFilterDto? filter = null)
    {
        try
        {
            var queryString = BuildQueryString(filter);
            var response = await _apiService.GetAsync<PagedResultDto<EventDto>>($"api/events{queryString}");
            return response?.Data?.Items ?? new List<EventDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting events: {ex.Message}");
            return new List<EventDto>();
        }
    }

    public async Task<EventDto?> GetEventByIdAsync(int eventId)
    {
        try
        {
            var response = await _apiService.GetAsync<EventDto>($"api/events/{eventId}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event {eventId}: {ex.Message}");
            return null;
        }
    }

    public async Task<EventDto?> GetEventBySlugAsync(string slug)
    {
        try
        {
            var response = await _apiService.GetAsync<EventDto>($"api/events/slug/{slug}");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event by slug {slug}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<EventDto>> GetMyEventsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<EventDto>>("api/events/my-events");
            
            if (response == null)
            {
                throw new Exception("No response received from server");
            }
            
            if (!response.Success)
            {
                throw new Exception(response.Message ?? "Failed to retrieve events");
            }
            
            return response.Data ?? new List<EventDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting my events: {ex.Message}");
            throw; // Re-throw so the UI can handle the error properly
        }
    }

    public async Task<EventDto?> CreateEventAsync(CreateEventDto createEventDto)
    {
        try
        {
            var response = await _apiService.PostAsync<EventDto>("api/events", createEventDto);
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating event: {ex.Message}");
            return null;
        }
    }

    public async Task<List<CategoryDto>> GetCategoriesAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<CategoryDto>>("api/categories/active");
            return response?.Data ?? new List<CategoryDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting categories: {ex.Message}");
            return new List<CategoryDto>();
        }
    }

    public async Task<List<VenueDto>> GetVenuesAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<VenueDto>>("api/venues");
            return response?.Data ?? new List<VenueDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting venues: {ex.Message}");
            return new List<VenueDto>();
        }
    }

    private string BuildQueryString(EventFilterDto? filter)
    {
        if (filter == null) return "";

        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");

        if (filter.CategoryID.HasValue)
            queryParams.Add($"categoryID={filter.CategoryID}");

        if (filter.PageNumber > 1)
            queryParams.Add($"pageNumber={filter.PageNumber}");

        if (filter.PageSize != 10)
            queryParams.Add($"pageSize={filter.PageSize}");

        return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
    }

    // Image upload methods
    public async Task<ApiResponse<string>?> UploadEventImageAsync(MultipartFormDataContent content)
    {
        try
        {
            var httpClient = _apiService.GetHttpClient();
            var response = await httpClient.PostAsync("api/fileupload/event-image", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ApiResponse<string>>(responseContent, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return ApiResponse<string>.ErrorResult($"Upload failed with status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading image: {ex.Message}");
            return ApiResponse<string>.ErrorResult($"Error uploading image: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>?> DeleteEventImageAsync(string imageUrl)
    {
        try
        {
            var success = await _apiService.DeleteAsync($"api/fileupload/event-image?imageUrl={Uri.EscapeDataString(imageUrl)}");
            if (success)
            {
                return ApiResponse<bool>.SuccessResult(true, "Image deleted successfully");
            }
            else
            {
                return ApiResponse<bool>.ErrorResult("Failed to delete image");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting image: {ex.Message}");
            return ApiResponse<bool>.ErrorResult($"Error deleting image: {ex.Message}");
        }
    }

    // Event management methods for organizers
    public async Task<List<EventDto>> GetEventsByOrganizerAsync(int organizerId)
    {
        try
        {
            var response = await _apiService.GetAsync<List<EventDto>>($"api/events/organizer/{organizerId}");
            return response?.Data ?? new List<EventDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting organizer events: {ex.Message}");
            return new List<EventDto>();
        }
    }

    public async Task<bool> UpdateEventAsync(EventDto eventDto)
    {
        try
        {
            var response = await _apiService.PutAsync<EventDto>($"api/events/{eventDto.EventID}", eventDto);
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating event: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateEventStatusAsync(int eventId, EventStatus status)
    {
        try
        {
            var response = await _apiService.PutAsync<bool>($"api/events/{eventId}/status", new { NewStatus = status });
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating event status: {ex.Message}");
            return false;
        }
    }

    public async Task<int> DuplicateEventAsync(int eventId)
    {
        try
        {
            var response = await _apiService.PostAsync<EventDto>($"api/events/{eventId}/duplicate", null);
            return response?.Data?.EventID ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error duplicating event: {ex.Message}");
            return 0;
        }
    }

    public async Task<bool> DeleteEventAsync(int eventId)
    {
        try
        {
            var success = await _apiService.DeleteAsync($"api/events/{eventId}");
            return success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting event: {ex.Message}");
            return false;
        }
    }

    // Image upload method for files
    public async Task<string?> UploadEventImageAsync(IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5MB limit
            using var streamContent = new StreamContent(stream);
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "file", file.Name);

            var response = await UploadEventImageAsync(content);
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file: {ex.Message}");
            return null;
        }
    }
}