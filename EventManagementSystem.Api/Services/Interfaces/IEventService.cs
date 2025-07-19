using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IEventService
    {
        Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventDto createEventDto, int organizerId);
        Task<ApiResponse<EventDto>> GetEventByIdAsync(int eventId);
        Task<ApiResponse<EventDto>> GetEventBySlugAsync(string slug);
        Task<ApiResponse<PagedResultDto<EventDto>>> GetEventsAsync(EventFilterDto filter);
        Task<ApiResponse<EventDto>> UpdateEventAsync(int eventId, CreateEventDto updateEventDto, int organizerId);
        Task<ApiResponse<bool>> DeleteEventAsync(int eventId, int organizerId);
        Task<ApiResponse<List<EventDto>>> GetUserEventsAsync(int organizerId);
        Task<ApiResponse<EventDto>> PublishEventAsync(int eventId, int organizerId);
        Task<ApiResponse<EventDto>> CancelEventAsync(int eventId, int organizerId, string reason);
    }
}
