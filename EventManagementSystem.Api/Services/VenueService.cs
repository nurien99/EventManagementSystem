using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Services
{
    public class VenueService : IVenueService
    {
        private readonly ApplicationDbContext _context;

        public VenueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<List<VenueDto>>> GetVenuesAsync()
        {
            try
            {
                var venues = await _context.Venues
                    .Include(v => v.Events)
                    .Where(v => v.IsActive)
                    .OrderBy(v => v.VenueName)
                    .ToListAsync();

                var venueDtos = venues.Select(MapToVenueDto).ToList();
                return ApiResponse<List<VenueDto>>.SuccessResult(venueDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VenueDto>>.ErrorResult("An error occurred while retrieving venues.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<VenueDto>> GetVenueByIdAsync(int venueId)
        {
            try
            {
                var venue = await _context.Venues
                    .Include(v => v.Events)
                    .FirstOrDefaultAsync(v => v.VenueID == venueId);

                if (venue == null)
                {
                    return ApiResponse<VenueDto>.ErrorResult("Venue not found.");
                }

                var venueDto = MapToVenueDto(venue);
                return ApiResponse<VenueDto>.SuccessResult(venueDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<VenueDto>.ErrorResult("An error occurred while retrieving the venue.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<VenueDto>> CreateVenueAsync(CreateVenueDto createVenueDto)
        {
            try
            {
                // Check if venue with same name and address already exists
                var existingVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.VenueName.ToLower() == createVenueDto.VenueName.ToLower() &&
                                            v.Address.ToLower() == createVenueDto.Address.ToLower() &&
                                            v.IsActive);

                if (existingVenue != null)
                {
                    return ApiResponse<VenueDto>.ErrorResult("A venue with this name and address already exists.");
                }

                var venue = new Venue
                {
                    VenueName = createVenueDto.VenueName,
                    Address = createVenueDto.Address,
                    City = createVenueDto.City ?? string.Empty,
                    State = createVenueDto.State ?? string.Empty,
                    PostalCode = createVenueDto.PostalCode ?? string.Empty,
                    Country = createVenueDto.Country ?? "Malaysia",
                    Capacity = createVenueDto.Capacity,
                    PhoneNumber = createVenueDto.PhoneNumber ?? string.Empty,
                    Email = createVenueDto.Email ?? string.Empty,
                    Website = createVenueDto.Website ?? string.Empty,
                    Facilities = createVenueDto.Facilities ?? string.Empty,
                    Description = createVenueDto.Description ?? string.Empty,
                    Latitude = createVenueDto.Latitude,
                    Longitude = createVenueDto.Longitude,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Venues.Add(venue);
                await _context.SaveChangesAsync();

                var venueDto = MapToVenueDto(venue);
                return ApiResponse<VenueDto>.SuccessResult(venueDto, "Venue created successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<VenueDto>.ErrorResult("An error occurred while creating the venue.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<VenueDto>> UpdateVenueAsync(int venueId, CreateVenueDto updateVenueDto)
        {
            try
            {
                var venue = await _context.Venues.FindAsync(venueId);
                if (venue == null)
                {
                    return ApiResponse<VenueDto>.ErrorResult("Venue not found.");
                }

                // Check if another venue with same name and address exists (excluding current venue)
                var existingVenue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.VenueName.ToLower() == updateVenueDto.VenueName.ToLower() &&
                                            v.Address.ToLower() == updateVenueDto.Address.ToLower() &&
                                            v.VenueID != venueId &&
                                            v.IsActive);

                if (existingVenue != null)
                {
                    return ApiResponse<VenueDto>.ErrorResult("A venue with this name and address already exists.");
                }

                // Update venue properties
                venue.VenueName = updateVenueDto.VenueName;
                venue.Address = updateVenueDto.Address;
                venue.City = updateVenueDto.City ?? string.Empty;
                venue.State = updateVenueDto.State ?? string.Empty;
                venue.PostalCode = updateVenueDto.PostalCode ?? string.Empty;
                venue.Country = updateVenueDto.Country ?? "Malaysia";
                venue.Capacity = updateVenueDto.Capacity;
                venue.PhoneNumber = updateVenueDto.PhoneNumber ?? string.Empty;
                venue.Email = updateVenueDto.Email ?? string.Empty;
                venue.Website = updateVenueDto.Website ?? string.Empty;
                venue.Facilities = updateVenueDto.Facilities ?? string.Empty;
                venue.Description = updateVenueDto.Description ?? string.Empty;
                venue.Latitude = updateVenueDto.Latitude;
                venue.Longitude = updateVenueDto.Longitude;
                venue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var venueDto = MapToVenueDto(venue);
                return ApiResponse<VenueDto>.SuccessResult(venueDto, "Venue updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<VenueDto>.ErrorResult("An error occurred while updating the venue.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteVenueAsync(int venueId)
        {
            try
            {
                var venue = await _context.Venues
                    .Include(v => v.Events)
                    .FirstOrDefaultAsync(v => v.VenueID == venueId);

                if (venue == null)
                {
                    return ApiResponse<bool>.ErrorResult("Venue not found.");
                }

                // Check if venue has events
                if (venue.Events.Any())
                {
                    return ApiResponse<bool>.ErrorResult("Cannot delete venue with existing events. Consider deactivating it instead.");
                }

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Venue deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while deleting the venue.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<VenueDto>>> SearchVenuesAsync(string searchTerm)
        {
            try
            {
                var query = _context.Venues
                    .Include(v => v.Events)
                    .Where(v => v.IsActive);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    query = query.Where(v => v.VenueName.Contains(searchTerm) ||
                                           v.City.Contains(searchTerm) ||
                                           v.Address.Contains(searchTerm) ||
                                           v.Description.Contains(searchTerm));
                }

                var venues = await query
                    .OrderBy(v => v.VenueName)
                    .ToListAsync();

                var venueDtos = venues.Select(MapToVenueDto).ToList();
                return ApiResponse<List<VenueDto>>.SuccessResult(venueDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VenueDto>>.ErrorResult("An error occurred while searching venues.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<VenueDto>>> GetVenuesByCityAsync(string city)
        {
            try
            {
                var venues = await _context.Venues
                    .Include(v => v.Events)
                    .Where(v => v.IsActive && v.City.ToLower() == city.ToLower())
                    .OrderBy(v => v.VenueName)
                    .ToListAsync();

                var venueDtos = venues.Select(MapToVenueDto).ToList();
                return ApiResponse<List<VenueDto>>.SuccessResult(venueDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<VenueDto>>.ErrorResult("An error occurred while retrieving venues by city.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ToggleVenueStatusAsync(int venueId, bool isActive)
        {
            try
            {
                var venue = await _context.Venues.FindAsync(venueId);
                if (venue == null)
                {
                    return ApiResponse<bool>.ErrorResult("Venue not found.");
                }

                venue.IsActive = isActive;
                venue.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var statusText = isActive ? "activated" : "deactivated";
                return ApiResponse<bool>.SuccessResult(true, $"Venue {statusText} successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while updating venue status.", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private VenueDto MapToVenueDto(Venue venue)
        {
            return new VenueDto
            {
                VenueID = venue.VenueID,
                VenueName = venue.VenueName,
                Address = venue.Address,
                City = venue.City,
                State = venue.State,
                PostalCode = venue.PostalCode,
                Country = venue.Country,
                Capacity = venue.Capacity,
                PhoneNumber = venue.PhoneNumber,
                Email = venue.Email,
                Website = venue.Website,
                Facilities = venue.Facilities,
                Description = venue.Description,
                Latitude = venue.Latitude,
                Longitude = venue.Longitude,
                IsActive = venue.IsActive,
                CreatedAt = venue.CreatedAt,
                UpdatedAt = venue.UpdatedAt,
                FullAddress = venue.FullAddress,
                HasCoordinates = venue.HasCoordinates,
                EventCount = venue.Events?.Count ?? 0
            };
        }

        #endregion
    }
}