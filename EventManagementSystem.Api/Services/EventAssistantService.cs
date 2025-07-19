using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Api.Services
{
    public class EventAssistantService : IEventAssistantService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventAssistantService> _logger;

        public EventAssistantService(ApplicationDbContext context, ILogger<EventAssistantService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ApiResponse<EventAssistantDto>> AssignAssistantAsync(AssignAssistantDto assignDto, int assignedByUserId)
        {
            try
            {
                // Check if event exists and user is organizer
                var eventEntity = await _context.Events.FindAsync(assignDto.EventID);
                if (eventEntity == null)
                {
                    return ApiResponse<EventAssistantDto>.ErrorResult("Event not found");
                }

                if (eventEntity.UserID != assignedByUserId)
                {
                    return ApiResponse<EventAssistantDto>.ErrorResult("Only event organizers can assign assistants");
                }

                // Find user by email
                var assistantUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == assignDto.AssistantEmail && u.IsActive);

                if (assistantUser == null)
                {
                    return ApiResponse<EventAssistantDto>.ErrorResult("User not found or inactive");
                }

                // Check if already assigned
                var existingAssignment = await _context.EventAssistants
                    .FirstOrDefaultAsync(ea => ea.EventID == assignDto.EventID && ea.UserID == assistantUser.UserID);

                if (existingAssignment != null)
                {
                    if (existingAssignment.IsActive)
                    {
                        return ApiResponse<EventAssistantDto>.ErrorResult("User is already assigned to this event");
                    }
                    else
                    {
                        // Reactivate existing assignment
                        existingAssignment.IsActive = true;
                        existingAssignment.Role = assignDto.Role;
                        existingAssignment.AssignedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        var reactivatedDto = await MapToEventAssistantDto(existingAssignment);
                        return ApiResponse<EventAssistantDto>.SuccessResult(reactivatedDto, "Assistant reactivated successfully");
                    }
                }

                // Create new assignment
                var eventAssistant = new EventAssistant
                {
                    EventID = assignDto.EventID,
                    UserID = assistantUser.UserID,
                    Role = assignDto.Role,
                    AssignedByUserID = assignedByUserId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.EventAssistants.Add(eventAssistant);
                await _context.SaveChangesAsync();

                var assistantDto = await MapToEventAssistantDto(eventAssistant);
                _logger.LogInformation("✅ Assistant assigned: {AssistantEmail} to event {EventId}", assignDto.AssistantEmail, assignDto.EventID);

                return ApiResponse<EventAssistantDto>.SuccessResult(assistantDto, "Assistant assigned successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error assigning assistant");
                return ApiResponse<EventAssistantDto>.ErrorResult("An error occurred while assigning assistant", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<EventAssistantDto>>> GetEventAssistantsAsync(int eventId)
        {
            try
            {
                var assistants = await _context.EventAssistants
                    .Include(ea => ea.Assistant)
                    .Include(ea => ea.AssignedBy)
                    .Where(ea => ea.EventID == eventId && ea.IsActive)
                    .OrderBy(ea => ea.AssignedAt)
                    .ToListAsync();

                var assistantDtos = new List<EventAssistantDto>();
                foreach (var assistant in assistants)
                {
                    assistantDtos.Add(await MapToEventAssistantDto(assistant));
                }

                return ApiResponse<List<EventAssistantDto>>.SuccessResult(assistantDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving event assistants for event {EventId}", eventId);
                return ApiResponse<List<EventAssistantDto>>.ErrorResult("An error occurred while retrieving assistants", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> CanUserManageEventAsync(int userId, int eventId)
        {
            try
            {
                // Check if user is the event organizer
                var isOrganizer = await _context.Events
                    .AnyAsync(e => e.EventID == eventId && e.UserID == userId);

                if (isOrganizer)
                {
                    return ApiResponse<bool>.SuccessResult(true, "User is event organizer");
                }

                // Check if user is assistant with management rights
                var hasManagementRights = await _context.EventAssistants
                    .AnyAsync(ea => ea.EventID == eventId &&
                                   ea.UserID == userId &&
                                   ea.IsActive &&
                                   (ea.Role == AssistantRole.FullAssistant || ea.Role == AssistantRole.ViewAttendees));

                return ApiResponse<bool>.SuccessResult(hasManagementRights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking management permissions");
                return ApiResponse<bool>.ErrorResult("Error checking permissions");
            }
        }

        public async Task<ApiResponse<bool>> CanUserCheckInTicketsAsync(int userId, int eventId)
        {
            try
            {
                // Check if user is the event organizer
                var isOrganizer = await _context.Events
                    .AnyAsync(e => e.EventID == eventId && e.UserID == userId);

                if (isOrganizer)
                {
                    return ApiResponse<bool>.SuccessResult(true, "User is event organizer");
                }

                // Check if user is assistant with check-in rights
                var hasCheckInRights = await _context.EventAssistants
                    .AnyAsync(ea => ea.EventID == eventId &&
                                   ea.UserID == userId &&
                                   ea.IsActive);

                return ApiResponse<bool>.SuccessResult(hasCheckInRights);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking check-in permissions");
                return ApiResponse<bool>.ErrorResult("Error checking permissions");
            }
        }

        public async Task<ApiResponse<bool>> RemoveAssistantAsync(int assistantId, int removedByUserId)
        {
            try
            {
                var assistant = await _context.EventAssistants
                    .Include(ea => ea.Event)
                    .FirstOrDefaultAsync(ea => ea.EventAssistantID == assistantId);

                if (assistant == null)
                {
                    return ApiResponse<bool>.ErrorResult("Assistant assignment not found");
                }

                // Only event organizer can remove assistants
                if (assistant.Event.UserID != removedByUserId)
                {
                    return ApiResponse<bool>.ErrorResult("Only event organizers can remove assistants");
                }

                assistant.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("🗑️ Assistant removed from event {EventId}", assistant.EventID);
                return ApiResponse<bool>.SuccessResult(true, "Assistant removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error removing assistant");
                return ApiResponse<bool>.ErrorResult("An error occurred while removing assistant", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventAssistantDto>> UpdateAssistantRoleAsync(int assistantId, UpdateAssistantRoleDto updateDto, int updatedByUserId)
        {
            try
            {
                var assistant = await _context.EventAssistants
                    .Include(ea => ea.Event)
                    .FirstOrDefaultAsync(ea => ea.EventAssistantID == assistantId);

                if (assistant == null)
                {
                    return ApiResponse<EventAssistantDto>.ErrorResult("Assistant assignment not found");
                }

                // Only event organizer can update roles
                if (assistant.Event.UserID != updatedByUserId)
                {
                    return ApiResponse<EventAssistantDto>.ErrorResult("Only event organizers can update assistant roles");
                }

                assistant.Role = updateDto.Role;
                await _context.SaveChangesAsync();

                var assistantDto = await MapToEventAssistantDto(assistant);
                _logger.LogInformation("🔄 Assistant role updated for event {EventId}", assistant.EventID);

                return ApiResponse<EventAssistantDto>.SuccessResult(assistantDto, "Assistant role updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating assistant role");
                return ApiResponse<EventAssistantDto>.ErrorResult("An error occurred while updating assistant role", new List<string> { ex.Message });
            }
        }

        private async Task<EventAssistantDto> MapToEventAssistantDto(EventAssistant assistant)
        {
            // Load related entities if not already loaded
            if (assistant.Assistant == null)
            {
                await _context.Entry(assistant)
                    .Reference(ea => ea.Assistant)
                    .LoadAsync();
            }

            if (assistant.AssignedBy == null)
            {
                await _context.Entry(assistant)
                    .Reference(ea => ea.AssignedBy)
                    .LoadAsync();
            }

            return new EventAssistantDto
            {
                EventAssistantID = assistant.EventAssistantID,
                EventID = assistant.EventID,
                UserID = assistant.UserID,
                UserName = assistant.Assistant?.Name ?? "Unknown",
                UserEmail = assistant.Assistant?.Email ?? "Unknown",
                Role = assistant.Role,
                AssignedAt = assistant.AssignedAt,
                AssignedByUserName = assistant.AssignedBy?.Name ?? "Unknown",
                IsActive = assistant.IsActive
            };
        }
    }
}