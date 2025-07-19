using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VenuesController : ControllerBase
    {
        private readonly IVenueService _venueService;

        public VenuesController(IVenueService venueService)
        {
            _venueService = venueService;
        }

        /// <summary>
        /// Get all active venues
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<VenueDto>>>> GetAllVenues()
        {
            var result = await _venueService.GetVenuesAsync();
            return Ok(result);
        }

        /// <summary>
        /// Get venue by ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<VenueDto>>> GetVenueById(int id)
        {
            var result = await _venueService.GetVenueByIdAsync(id);

            if (!result.Success)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Create a new venue (Admin/EventOrganizer only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,EventOrganizer")]
        public async Task<ActionResult<ApiResponse<VenueDto>>> CreateVenue([FromBody] CreateVenueDto createVenueDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<VenueDto>.ErrorResult("Validation failed.", errors));
            }

            var result = await _venueService.CreateVenueAsync(createVenueDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return CreatedAtAction(nameof(GetVenueById), new { id = result.Data.VenueID }, result);
        }

        /// <summary>
        /// Update an existing venue (Admin/EventOrganizer only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,EventOrganizer")]
        public async Task<ActionResult<ApiResponse<VenueDto>>> UpdateVenue(int id, [FromBody] CreateVenueDto updateVenueDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<VenueDto>.ErrorResult("Validation failed.", errors));
            }

            var result = await _venueService.UpdateVenueAsync(id, updateVenueDto);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete a venue (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteVenue(int id)
        {
            var result = await _venueService.DeleteVenueAsync(id);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        /// <summary>
        /// Search venues by name, city, or description
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<VenueDto>>>> SearchVenues([FromQuery] string searchTerm)
        {
            var result = await _venueService.SearchVenuesAsync(searchTerm);
            return Ok(result);
        }

        /// <summary>
        /// Get venues by city
        /// </summary>
        [HttpGet("city/{city}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<VenueDto>>>> GetVenuesByCity(string city)
        {
            var result = await _venueService.GetVenuesByCityAsync(city);
            return Ok(result);
        }

        /// <summary>
        /// Toggle venue active status (Admin only)
        /// </summary>
        [HttpPut("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> ToggleVenueStatus(int id, [FromBody] bool isActive)
        {
            var result = await _venueService.ToggleVenueStatusAsync(id, isActive);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}