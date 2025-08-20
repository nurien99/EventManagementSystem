using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface ITicketService
    {
        Task<ApiResponse<CheckInTicketResponse>> CheckInTicketAsync(CheckInTicketRequest request, int checkInUserId);
        Task<ApiResponse<ValidateTicketResponse>> ValidateTicketAsync(ValidateTicketRequest request);
        Task<ApiResponse<TicketCheckInDetails>> GetTicketDetailsByCodeAsync(string ticketCode);
        Task<ApiResponse<List<TicketCheckInDetails>>> GetEventCheckInsAsync(int eventId);
        Task<ApiResponse<bool>> UndoCheckInAsync(int ticketId, int userId);
        Task<ApiResponse<List<UserTicketDto>>> GetUserTicketsAsync(int userId);
    }
}