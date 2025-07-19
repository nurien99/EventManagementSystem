using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IVenueService
    {
        Task<ApiResponse<List<VenueDto>>> GetVenuesAsync();
        Task<ApiResponse<VenueDto>> GetVenueByIdAsync(int venueId);
        Task<ApiResponse<VenueDto>> CreateVenueAsync(CreateVenueDto createVenueDto);
        Task<ApiResponse<VenueDto>> UpdateVenueAsync(int venueId, CreateVenueDto updateVenueDto);
        Task<ApiResponse<bool>> DeleteVenueAsync(int venueId);
        Task<ApiResponse<List<VenueDto>>> SearchVenuesAsync(string searchTerm);
        Task<ApiResponse<List<VenueDto>>> GetVenuesByCityAsync(string city);
        Task<ApiResponse<bool>> ToggleVenueStatusAsync(int venueId, bool isActive);
    }
}