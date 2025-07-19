using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IEventAssistantService
    {
        Task<ApiResponse<EventAssistantDto>> AssignAssistantAsync(AssignAssistantDto assignDto, int assignedByUserId);
        Task<ApiResponse<List<EventAssistantDto>>> GetEventAssistantsAsync(int eventId);
        Task<ApiResponse<EventAssistantDto>> UpdateAssistantRoleAsync(int assistantId, UpdateAssistantRoleDto updateDto, int updatedByUserId);
        Task<ApiResponse<bool>> RemoveAssistantAsync(int assistantId, int removedByUserId);
        Task<ApiResponse<bool>> CanUserManageEventAsync(int userId, int eventId);
        Task<ApiResponse<bool>> CanUserCheckInTicketsAsync(int userId, int eventId);
    }
}