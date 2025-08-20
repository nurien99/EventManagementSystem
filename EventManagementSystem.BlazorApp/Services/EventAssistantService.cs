using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.BlazorApp.Services
{
    public class EventAssistantService
    {
        private readonly HttpClient _httpClient;

        public EventAssistantService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("EventManagementAPI");
        }

        /// <summary>
        /// Get all assistants for an event
        /// </summary>
        public async Task<List<EventAssistantDto>?> GetEventAssistantsAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<EventAssistantDto>>>($"api/eventassistants/event/{eventId}");
                
                if (response?.Success == true)
                {
                    return response.Data;
                }
                
                return new List<EventAssistantDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting event assistants: {ex.Message}");
                return new List<EventAssistantDto>();
            }
        }

        /// <summary>
        /// Assign a new assistant to an event
        /// </summary>
        public async Task<ApiResponse<EventAssistantDto>?> AssignAssistantAsync(AssignAssistantDto assignDto)
        {
            try
            {
                Console.WriteLine($"🔵 Assigning assistant: {assignDto.AssistantEmail} to event {assignDto.EventID} with role {assignDto.Role}");
                
                var response = await _httpClient.PostAsJsonAsync("api/eventassistants/assign", assignDto);
                
                Console.WriteLine($"🔵 Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponse<EventAssistantDto>>();
                    Console.WriteLine($"🔵 API Response: Success={result?.Success}, Message={result?.Message}");
                    return result;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"🔴 API Error: {response.StatusCode} - {errorContent}");
                    
                    // Try to parse the error response
                    try
                    {
                        var errorResponse = await response.Content.ReadFromJsonAsync<ApiResponse<EventAssistantDto>>();
                        return errorResponse ?? new ApiResponse<EventAssistantDto> { Success = false, Message = $"HTTP {response.StatusCode}: {errorContent}" };
                    }
                    catch
                    {
                        return new ApiResponse<EventAssistantDto> { Success = false, Message = $"HTTP {response.StatusCode}: {errorContent}" };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 Exception assigning assistant: {ex.Message}");
                return new ApiResponse<EventAssistantDto> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Update an assistant's role
        /// </summary>
        public async Task<ApiResponse<EventAssistantDto>?> UpdateAssistantRoleAsync(int assistantId, UpdateAssistantRoleDto updateDto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/eventassistants/{assistantId}/role", updateDto);
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<EventAssistantDto>>();
                }
                
                return new ApiResponse<EventAssistantDto> { Success = false, Message = "Failed to update assistant role" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating assistant role: {ex.Message}");
                return new ApiResponse<EventAssistantDto> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Remove/deactivate an assistant
        /// </summary>
        public async Task<ApiResponse<bool>?> RemoveAssistantAsync(int assistantId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/eventassistants/{assistantId}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<bool>>();
                }
                
                return new ApiResponse<bool> { Success = false, Message = "Failed to remove assistant" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing assistant: {ex.Message}");
                return new ApiResponse<bool> { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// Get assistants assigned to current user for different events
        /// </summary>
        public async Task<List<EventAssistantDto>?> GetMyAssistantRolesAsync()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<EventAssistantDto>>>("api/eventassistants/my-roles");
                
                if (response?.Success == true)
                {
                    return response.Data;
                }
                
                return new List<EventAssistantDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting my assistant roles: {ex.Message}");
                return new List<EventAssistantDto>();
            }
        }
    }
}