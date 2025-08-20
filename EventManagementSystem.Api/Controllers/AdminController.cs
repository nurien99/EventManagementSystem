using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(ApplicationDbContext context, ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all users for admin management
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<ApiResponse<List<AdminUserDto>>>> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new AdminUserDto
                    {
                        UserID = u.UserID,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role,
                        IsEmailVerified = u.IsEmailVerified,
                        DateRegistered = u.CreatedAt ?? DateTime.MinValue,
                        EventsCreated = _context.Events.Count(e => e.UserID == u.UserID),
                        EventsAttended = _context.Registrations.Count(r => r.UserID == u.UserID && r.Status == RegistrationStatus.Confirmed)
                    })
                    .OrderByDescending(u => u.DateRegistered)
                    .ToListAsync();

                return Ok(new ApiResponse<List<AdminUserDto>>
                {
                    Success = true,
                    Data = users,
                    Message = $"Retrieved {users.Count} users successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users for admin");
                return StatusCode(500, new ApiResponse<List<AdminUserDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve users"
                });
            }
        }

        /// <summary>
        /// Get user details by ID
        /// </summary>
        [HttpGet("users/{userId}")]
        public async Task<ActionResult<ApiResponse<AdminUserDetailDto>>> GetUserDetails(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.UserID == userId)
                    .Select(u => new AdminUserDetailDto
                    {
                        UserID = u.UserID,
                        Name = u.Name,
                        Email = u.Email,
                        Role = u.Role,
                        IsEmailVerified = u.IsEmailVerified,
                        DateRegistered = u.CreatedAt ?? DateTime.MinValue,
                        LastLoginDate = null, // Not tracked in current User entity
                        EventsCreated = _context.Events.Where(e => e.UserID == u.UserID)
                            .Select(e => new AdminEventSummaryDto
                            {
                                EventID = e.EventID,
                                Name = e.EventName,
                                Status = e.Status,
                                StartDate = e.StartDate
                            }).ToList(),
                        Registrations = _context.Registrations
                            .Where(r => r.UserID == u.UserID)
                            .Include(r => r.Event)
                            .Select(r => new AdminRegistrationDto
                            {
                                RegistrationID = r.RegisterID,
                                EventName = r.Event.EventName,
                                RegistrationDate = r.RegisteredAt ?? DateTime.MinValue,
                                Status = r.Status
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new ApiResponse<AdminUserDetailDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                return Ok(new ApiResponse<AdminUserDetailDto>
                {
                    Success = true,
                    Data = user,
                    Message = "User details retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user details for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<AdminUserDetailDto>
                {
                    Success = false,
                    Message = "Failed to retrieve user details"
                });
            }
        }

        /// <summary>
        /// Update user role
        /// </summary>
        [HttpPut("users/{userId}/role")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateUserRole(int userId, [FromBody] UpdateUserRoleDto request)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // Don't allow changing the last admin's role
                if (user.Role == UserRole.Admin)
                {
                    var adminCount = await _context.Users.CountAsync(u => u.Role == UserRole.Admin);
                    if (adminCount <= 1 && request.NewRole != UserRole.Admin)
                    {
                        return BadRequest(new ApiResponse<string>
                        {
                            Success = false,
                            Message = "Cannot change the role of the last administrator"
                        });
                    }
                }

                var oldRole = user.Role;
                user.Role = request.NewRole;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} role changed from {OldRole} to {NewRole} by admin", 
                    userId, oldRole, request.NewRole);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = $"User role updated from {oldRole} to {request.NewRole} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to update user role"
                });
            }
        }

        /// <summary>
        /// Get platform statistics for admin dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<ApiResponse<AdminStatsDto>>> GetPlatformStats()
        {
            try
            {
                var stats = new AdminStatsDto
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalEvents = await _context.Events.CountAsync(),
                    TotalRegistrations = await _context.Registrations.CountAsync(),
                    TotalRevenue = 0, // TODO: Calculate when payment system is implemented

                    UsersByRole = new Dictionary<string, int>
                    {
                        ["Attendee"] = await _context.Users.CountAsync(u => u.Role == UserRole.Attendee),
                        ["EventOrganizer"] = await _context.Users.CountAsync(u => u.Role == UserRole.EventOrganizer),
                        ["Admin"] = await _context.Users.CountAsync(u => u.Role == UserRole.Admin)
                    },

                    EventsByStatus = new Dictionary<string, int>
                    {
                        ["Draft"] = await _context.Events.CountAsync(e => e.Status == EventStatus.Draft),
                        ["Published"] = await _context.Events.CountAsync(e => e.Status == EventStatus.Published),
                        ["InProgress"] = await _context.Events.CountAsync(e => e.Status == EventStatus.InProgress),
                        ["Completed"] = await _context.Events.CountAsync(e => e.Status == EventStatus.Completed),
                        ["Cancelled"] = await _context.Events.CountAsync(e => e.Status == EventStatus.Cancelled)
                    },

                    RecentActivity = await GetRecentActivity()
                };

                return Ok(new ApiResponse<AdminStatsDto>
                {
                    Success = true,
                    Data = stats,
                    Message = "Platform statistics retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving platform statistics");
                return StatusCode(500, new ApiResponse<AdminStatsDto>
                {
                    Success = false,
                    Message = "Failed to retrieve platform statistics"
                });
            }
        }

        private async Task<List<AdminActivityDto>> GetRecentActivity()
        {
            var recentActivity = new List<AdminActivityDto>();

            // Recent user registrations
            var recentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .Select(u => new AdminActivityDto
                {
                    Type = "User Registration",
                    Description = $"{u.Name} registered as {u.Role}",
                    Timestamp = u.CreatedAt ?? DateTime.MinValue,
                    UserId = u.UserID,
                    UserName = u.Name
                })
                .ToListAsync();

            recentActivity.AddRange(recentUsers);

            // Recent events
            var recentEvents = await _context.Events
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .Select(e => new AdminActivityDto
                {
                    Type = "Event Created",
                    Description = $"{e.Organizer.Name} created event '{e.EventName}'",
                    Timestamp = e.CreatedAt ?? DateTime.MinValue,
                    UserId = e.UserID,
                    UserName = e.Organizer.Name,
                    EventId = e.EventID,
                    EventName = e.EventName
                })
                .ToListAsync();

            recentActivity.AddRange(recentEvents);

            // Recent registrations
            var recentRegistrations = await _context.Registrations
                .Include(r => r.User)
                .Include(r => r.Event)
                .OrderByDescending(r => r.RegisteredAt)
                .Take(5)
                .Select(r => new AdminActivityDto
                {
                    Type = "Event Registration",
                    Description = $"{r.User.Name} registered for '{r.Event.EventName}'",
                    Timestamp = r.RegisteredAt ?? DateTime.MinValue,
                    UserId = r.UserID,
                    UserName = r.User.Name,
                    EventId = r.EventID,
                    EventName = r.Event.EventName
                })
                .ToListAsync();

            recentActivity.AddRange(recentRegistrations);

            return recentActivity
                .OrderByDescending(a => a.Timestamp)
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// Get events for admin management
        /// </summary>
        [HttpGet("events")]
        public async Task<ActionResult<ApiResponse<List<AdminEventDto>>>> GetAllEvents()
        {
            try
            {
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .Include(e => e.Category)
                    .Select(e => new AdminEventDto
                    {
                        EventID = e.EventID,
                        Name = e.EventName,
                        Status = e.Status,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate ?? e.StartDate,
                        MaxCapacity = e.MaxCapacity ?? 0,
                        AvailableTickets = e.MaxCapacity ?? 0, // Will need proper calculation later
                        OrganizerName = e.Organizer.Name,
                        OrganizerEmail = e.Organizer.Email,
                        VenueName = e.Venue != null ? e.Venue.VenueName : "TBD",
                        CategoryName = e.Category != null ? e.Category.CategoryName : "Uncategorized",
                        TotalRegistrations = _context.Registrations.Count(r => r.EventID == e.EventID),
                        CreatedDate = e.CreatedAt ?? DateTime.MinValue
                    })
                    .OrderByDescending(e => e.CreatedDate)
                    .ToListAsync();

                return Ok(new ApiResponse<List<AdminEventDto>>
                {
                    Success = true,
                    Data = events,
                    Message = $"Retrieved {events.Count} events successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for admin");
                return StatusCode(500, new ApiResponse<List<AdminEventDto>>
                {
                    Success = false,
                    Message = "Failed to retrieve events"
                });
            }
        }

        /// <summary>
        /// Update event status (admin override)
        /// </summary>
        [HttpPut("events/{eventId}/status")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateEventStatus(int eventId, [FromBody] UpdateEventStatusDto request)
        {
            try
            {
                var eventItem = await _context.Events
                    .Include(e => e.Organizer)
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventItem == null)
                {
                    return NotFound(new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Event not found"
                    });
                }

                var oldStatus = eventItem.Status;
                eventItem.Status = request.NewStatus;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Event {EventId} status changed from {OldStatus} to {NewStatus} by admin", 
                    eventId, oldStatus, request.NewStatus);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = $"Event status updated from {oldStatus} to {request.NewStatus} successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event status for event {EventId}", eventId);
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = "Failed to update event status"
                });
            }
        }
    }
}