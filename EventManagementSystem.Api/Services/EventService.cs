using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace EventManagementSystem.Api.Services
{
    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;

        public EventService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<EventDto>> CreateEventAsync(CreateEventDto createEventDto, int organizerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

try
{
    // Validate organizer exists
    var organizer = await _context.Users.FindAsync(organizerId);
    if (organizer == null)
    {
        return ApiResponse<EventDto>.ErrorResult("Organizer not found.");
    }

    // Handle venue creation or selection
    int venueId;
    if (createEventDto.VenueID.HasValue)
    {
        // Use existing venue
        venueId = createEventDto.VenueID.Value;
        var existingVenue = await _context.Venues.FindAsync(venueId);
        if (existingVenue == null)
        {
            return ApiResponse<EventDto>.ErrorResult("Selected venue not found.");
        }
    }
    else
    {
        // Create new venue
        if (string.IsNullOrWhiteSpace(createEventDto.NewVenueName) || string.IsNullOrWhiteSpace(createEventDto.NewVenueAddress))
        {
            return ApiResponse<EventDto>.ErrorResult("Venue information is required.");
        }

        var newVenue = new Venue
        {
            VenueName = createEventDto.NewVenueName,
            Address = createEventDto.NewVenueAddress,
            City = createEventDto.NewVenueCity ?? string.Empty,
            State = createEventDto.NewVenueState ?? string.Empty,
            PostalCode = createEventDto.NewVenuePostalCode ?? string.Empty,
            Country = "Malaysia",
            PhoneNumber = string.Empty,
            Email = string.Empty,
            Website = string.Empty,
            Facilities = string.Empty,
            Description = string.Empty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Venues.Add(newVenue);
        // ✅ NO SaveChangesAsync here - keep in transaction memory!
        // Entity Framework will generate IDs when we call SaveChanges at the end
        venueId = newVenue.VenueID; // Will be 0 temporarily, but EF will set it during SaveChanges
    }

    // Generate unique URL slug
    var urlSlug = await GenerateUniqueSlugAsync(createEventDto.EventName);

    // Create the event
    var newEvent = new Event
    {
        EventName = createEventDto.EventName,
        EventDesc = createEventDto.EventDesc ?? string.Empty,
        StartDate = createEventDto.StartDate,
        EndDate = createEventDto.EndDate,
        UserID = organizerId,
        VenueID = venueId,
        Status = EventStatus.Draft,
        MaxCapacity = createEventDto.MaxCapacity,
        RegistrationDeadline = createEventDto.RegistrationDeadline,
        ImageUrl = createEventDto.ImageUrl ?? string.Empty,
        CategoryID = createEventDto.CategoryID,
        UrlSlug = urlSlug,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = null
    };

    _context.Events.Add(newEvent);
    // ✅ NO SaveChangesAsync here - keep in transaction memory!
    // Entity Framework will generate IDs when we call SaveChanges at the end

    // Create ticket types if provided
    if (createEventDto.TicketTypes?.Any() == true)
    {
        foreach (var ticketTypeDto in createEventDto.TicketTypes)
        {
            var ticketType = new TicketType
            {
                Event = newEvent, // Use navigation property instead of ID
                TypeName = ticketTypeDto.TypeName,
                Description = ticketTypeDto.Description ?? string.Empty,
                Price = ticketTypeDto.Price,
                Quantity = ticketTypeDto.Quantity,
                SoldQuantity = 0,
                SaleStartDate = ticketTypeDto.SaleStartDate,
                SaleEndDate = ticketTypeDto.SaleEndDate,
                IsActive = true,
                DisplayOrder = ticketTypeDto.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TicketTypes.Add(ticketType);
        }
        // ✅ NO SaveChangesAsync here - accumulate all changes
    }

    // 🎯 SINGLE SAVE AND COMMIT - All operations succeed together
    await _context.SaveChangesAsync();  // Save all accumulated changes (generates all IDs)
    await transaction.CommitAsync();    // Commit the transaction

    // Return the created event with full details
    var eventDto = await GetEventWithDetailsAsync(newEvent.EventID);
    return ApiResponse<EventDto>.SuccessResult(eventDto, "Event created successfully.");
}
catch (Exception ex)
{
    // 🔄 ROLLBACK - If anything fails, undo everything
    await transaction.RollbackAsync();
    return ApiResponse<EventDto>.ErrorResult("An error occurred while creating the event.", new List<string> { ex.Message });
}  
        }

        public async Task<ApiResponse<EventDto>> GetEventByIdAsync(int eventId)
        {
            try
            {
                var eventDto = await GetEventWithDetailsAsync(eventId);
                if (eventDto == null)
                {
                    return ApiResponse<EventDto>.ErrorResult("Event not found.");
                }

                return ApiResponse<EventDto>.SuccessResult(eventDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<EventDto>.ErrorResult("An error occurred while retrieving the event.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventDto>> GetEventBySlugAsync(string slug)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .Include(e => e.Category)
                    .Include(e => e.TicketTypes)
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.UrlSlug == slug);

                if (eventEntity == null)
                {
                    return ApiResponse<EventDto>.ErrorResult("Event not found.");
                }

                var eventDto = MapToEventDto(eventEntity);
                return ApiResponse<EventDto>.SuccessResult(eventDto);
            }
            catch (Exception ex)
            {
                return ApiResponse<EventDto>.ErrorResult("An error occurred while retrieving the event.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<PagedResultDto<EventDto>>> GetEventsAsync(EventFilterDto filter)
        {
            try
            {
                var query = _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .Include(e => e.Category)
                    .Include(e => e.TicketTypes)
                    .Select(e => new
                    {
                        Event = e,
                        RegistrationCount = e.Registrations.Count()
                    })
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    query = query.Where(e => e.Event.EventName.Contains(filter.SearchTerm) ||
                                           e.Event.EventDesc.Contains(filter.SearchTerm));
                }

                if (filter.CategoryID.HasValue)
                {
                    query = query.Where(e => e.Event.CategoryID == filter.CategoryID.Value);
                }

                if (filter.VenueID.HasValue)
                {
                    query = query.Where(e => e.Event.VenueID == filter.VenueID.Value);
                }

                if (filter.Status.HasValue)
                {
                    query = query.Where(e => e.Event.Status == filter.Status.Value);
                }

                if (filter.StartDateFrom.HasValue)
                {
                    query = query.Where(e => e.Event.StartDate >= filter.StartDateFrom.Value);
                }

                if (filter.StartDateTo.HasValue)
                {
                    query = query.Where(e => e.Event.StartDate <= filter.StartDateTo.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.City))
                {
                    query = query.Where(e => e.Event.Venue.City.Contains(filter.City));
                }

                if (filter.OrganizerID.HasValue)
                {
                    query = query.Where(e => e.Event.UserID == filter.OrganizerID.Value);
                }

                if (filter.IsFree.HasValue && filter.IsFree.Value)
                {
                    query = query.Where(e => e.Event.TicketTypes.Any(t => t.Price == 0));
                }

                if (filter.HasAvailableSpots.HasValue && filter.HasAvailableSpots.Value)
                {
                    query = query.Where(e => !e.Event.MaxCapacity.HasValue ||
                                           e.RegistrationCount < e.Event.MaxCapacity.Value);
                }

                // Apply sorting
                switch (filter.SortBy?.ToLower())
                {
                    case "eventname":
                        query = filter.SortDirection?.ToLower() == "desc"
                            ? query.OrderByDescending(e => e.Event.EventName)
                            : query.OrderBy(e => e.Event.EventName);
                        break;
                    case "createdat":
                        query = filter.SortDirection?.ToLower() == "desc"
                            ? query.OrderByDescending(e => e.Event.CreatedAt)
                            : query.OrderBy(e => e.Event.CreatedAt);
                        break;
                    default: // StartDate
                        query = filter.SortDirection?.ToLower() == "desc"
                            ? query.OrderByDescending(e => e.Event.StartDate)
                            : query.OrderBy(e => e.Event.StartDate);
                        break;
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var results = await query
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // Map to DTOs
                var eventDtos = results.Select(r => new EventDto
                {
                    EventID = r.Event.EventID,
            EventName = r.Event.EventName,
            EventDesc = r.Event.EventDesc,
            StartDate = r.Event.StartDate,
            EndDate = r.Event.EndDate,
            Status = r.Event.Status,
            MaxCapacity = r.Event.MaxCapacity,
            RegistrationDeadline = r.Event.RegistrationDeadline,
            ImageUrl = r.Event.ImageUrl,
            UrlSlug = r.Event.UrlSlug,
            UserID = r.Event.UserID,
            OrganizerName = r.Event.Organizer?.Name ?? "Unknown",
            VenueID = r.Event.VenueID,
            VenueName = r.Event.Venue?.VenueName ?? "Unknown",
            VenueAddress = r.Event.Venue?.Address ?? string.Empty,
            CategoryID = r.Event.CategoryID,
            CategoryName = r.Event.Category?.CategoryName ?? string.Empty,
            TotalRegistrations = r.RegistrationCount, // Use pre-calculated count!
            AvailableSpots = r.Event.MaxCapacity.HasValue
                ? Math.Max(0, r.Event.MaxCapacity.Value - r.RegistrationCount)
                : int.MaxValue,
            CreatedAt = r.Event.CreatedAt,
            UpdatedAt = r.Event.UpdatedAt,
            TicketTypes = r.Event.TicketTypes?.Select(MapToTicketTypeDto).ToList() ?? new List<TicketTypeDto>()
            }).ToList();
                    
                var result = new PagedResultDto<EventDto>
                {
                    Items = eventDtos,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                return ApiResponse<PagedResultDto<EventDto>>.SuccessResult(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<PagedResultDto<EventDto>>.ErrorResult("An error occurred while retrieving events.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventDto>> UpdateEventAsync(int eventId, CreateEventDto updateEventDto, int organizerId)
        {
            try
            {
                var existingEvent = await _context.Events
                    .Include(e => e.TicketTypes)
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (existingEvent == null)
                {
                    return ApiResponse<EventDto>.ErrorResult("Event not found.");
                }

                // Check if user owns this event or is admin
                if (existingEvent.UserID != organizerId)
                {
                    return ApiResponse<EventDto>.ErrorResult("You can only update your own events.");
                }

                // Update event properties
                existingEvent.EventName = updateEventDto.EventName;
                existingEvent.EventDesc = updateEventDto.EventDesc ?? string.Empty;
                existingEvent.StartDate = updateEventDto.StartDate;
                existingEvent.EndDate = updateEventDto.EndDate;
                existingEvent.MaxCapacity = updateEventDto.MaxCapacity;
                existingEvent.RegistrationDeadline = updateEventDto.RegistrationDeadline;
                existingEvent.ImageUrl = updateEventDto.ImageUrl ?? string.Empty;
                existingEvent.CategoryID = updateEventDto.CategoryID;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                // Handle venue update if needed
                if (updateEventDto.VenueID.HasValue && updateEventDto.VenueID.Value != existingEvent.VenueID)
                {
                    var newVenue = await _context.Venues.FindAsync(updateEventDto.VenueID.Value);
                    if (newVenue == null)
                    {
                        return ApiResponse<EventDto>.ErrorResult("Selected venue not found.");
                    }
                    existingEvent.VenueID = updateEventDto.VenueID.Value;
                }

                await _context.SaveChangesAsync();

                var updatedEventDto = await GetEventWithDetailsAsync(eventId);
                return ApiResponse<EventDto>.SuccessResult(updatedEventDto, "Event updated successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventDto>.ErrorResult("An error occurred while updating the event.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> DeleteEventAsync(int eventId, int organizerId)
        {
            try
            {
                var existingEvent = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (existingEvent == null)
                {
                    return ApiResponse<bool>.ErrorResult("Event not found.");
                }

                // Check if user owns this event
                if (existingEvent.UserID != organizerId)
                {
                    return ApiResponse<bool>.ErrorResult("You can only delete your own events.");
                }

                // Check if event has registrations
                if (existingEvent.Registrations.Any())
                {
                    return ApiResponse<bool>.ErrorResult("Cannot delete event with existing registrations. Consider cancelling the event instead.");
                }

                _context.Events.Remove(existingEvent);
                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, "Event deleted successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult("An error occurred while deleting the event.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<EventDto>>> GetUserEventsAsync(int organizerId)
        {
            try
            {
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Venue)
                    .Include(e => e.Category)
                    .Include(e => e.TicketTypes)
                    .Include(e => e.Registrations)
                    .Where(e => e.UserID == organizerId)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                var eventDtos = events.Select(MapToEventDto).ToList();
                return ApiResponse<List<EventDto>>.SuccessResult(eventDtos);
            }
            catch (Exception ex)
            {
                return ApiResponse<List<EventDto>>.ErrorResult("An error occurred while retrieving user events.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventDto>> PublishEventAsync(int eventId, int organizerId)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                {
                    return ApiResponse<EventDto>.ErrorResult("Event not found.");
                }

                if (eventEntity.UserID != organizerId)
                {
                    return ApiResponse<EventDto>.ErrorResult("You can only publish your own events.");
                }

                eventEntity.Status = EventStatus.Published;
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var eventDto = await GetEventWithDetailsAsync(eventId);
                return ApiResponse<EventDto>.SuccessResult(eventDto, "Event published successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventDto>.ErrorResult("An error occurred while publishing the event.", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EventDto>> CancelEventAsync(int eventId, int organizerId, string reason)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null)
                {
                    return ApiResponse<EventDto>.ErrorResult("Event not found.");
                }

                if (eventEntity.UserID != organizerId)
                {
                    return ApiResponse<EventDto>.ErrorResult("You can only cancel your own events.");
                }

                eventEntity.Status = EventStatus.Cancelled;
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // TODO: Send cancellation emails to registered attendees
                // await _emailService.SendEventCancellationEmailsAsync(eventId, reason);

                var eventDto = await GetEventWithDetailsAsync(eventId);
                return ApiResponse<EventDto>.SuccessResult(eventDto, "Event cancelled successfully.");
            }
            catch (Exception ex)
            {
                return ApiResponse<EventDto>.ErrorResult("An error occurred while cancelling the event.", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<EventDto> GetEventWithDetailsAsync(int eventId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.EventID == eventId);

            return eventEntity != null ? MapToEventDto(eventEntity) : null;
        }

        private EventDto MapToEventDto(Event eventEntity)
        {
            return new EventDto
            {
                EventID = eventEntity.EventID,
                EventName = eventEntity.EventName,
                EventDesc = eventEntity.EventDesc,
                StartDate = eventEntity.StartDate,
                EndDate = eventEntity.EndDate,
                Status = eventEntity.Status,
                MaxCapacity = eventEntity.MaxCapacity,
                RegistrationDeadline = eventEntity.RegistrationDeadline,
                ImageUrl = eventEntity.ImageUrl,
                UrlSlug = eventEntity.UrlSlug,
                UserID = eventEntity.UserID,
                OrganizerName = eventEntity.Organizer?.Name ?? "Unknown",
                VenueID = eventEntity.VenueID,
                VenueName = eventEntity.Venue?.VenueName ?? "Unknown",
                VenueAddress = eventEntity.Venue?.Address ?? string.Empty,
                CategoryID = eventEntity.CategoryID,
                CategoryName = eventEntity.Category?.CategoryName ?? string.Empty,
                TotalRegistrations = eventEntity.Registrations?.Count ?? 0,
                AvailableSpots = eventEntity.MaxCapacity.HasValue
                    ? Math.Max(0, eventEntity.MaxCapacity.Value - (eventEntity.Registrations?.Count ?? 0))
                    : int.MaxValue,
                CreatedAt = eventEntity.CreatedAt,
                UpdatedAt = eventEntity.UpdatedAt,
                TicketTypes = eventEntity.TicketTypes?.Select(MapToTicketTypeDto).ToList() ?? new List<TicketTypeDto>()
            };
        }

        private TicketTypeDto MapToTicketTypeDto(TicketType ticketType)
        {
            return new TicketTypeDto
            {
                TicketTypeID = ticketType.TicketTypeID,
                EventID = ticketType.EventID,
                TypeName = ticketType.TypeName,
                Description = ticketType.Description,
                Price = ticketType.Price,
                Quantity = ticketType.Quantity,
                SoldQuantity = ticketType.SoldQuantity,
                SaleStartDate = ticketType.SaleStartDate,
                SaleEndDate = ticketType.SaleEndDate,
                IsActive = ticketType.IsActive
            };
        }

        private async Task<string> GenerateUniqueSlugAsync(string eventName)
        {
            // Create base slug from event name
            var baseSlug = CreateSlug(eventName);
            var slug = baseSlug;
            var counter = 1;

            // Ensure uniqueness
            while (await _context.Events.AnyAsync(e => e.UrlSlug == slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }

            return slug;
        }

        private string CreateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "event";

            // Convert to lowercase and replace spaces with hyphens
            var slug = input.ToLowerInvariant().Trim();

            // Remove special characters and keep only alphanumeric and hyphens
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            // Limit length
            if (slug.Length > 50)
                slug = slug.Substring(0, 50).TrimEnd('-');

            return string.IsNullOrWhiteSpace(slug) ? "event" : slug;
        }

        public async Task<ApiResponse<bool>> UpdateEventStatusAsync(int eventId, EventStatus status, int organizerId)
        {
            try
            {
                var eventEntity = await _context.Events
                    .FirstOrDefaultAsync(e => e.EventID == eventId);

                if (eventEntity == null)
                {
                    return ApiResponse<bool>.ErrorResult("Event not found.");
                }

                // Check if user owns this event or is an admin
                var user = await _context.Users.FindAsync(organizerId);
                if (user == null)
                {
                    return ApiResponse<bool>.ErrorResult("User not found.");
                }

                if (eventEntity.UserID != organizerId && user.Role != UserRole.Admin)
                {
                    return ApiResponse<bool>.ErrorResult("You don't have permission to modify this event.");
                }

                eventEntity.Status = status;
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ApiResponse<bool>.SuccessResult(true, 
                    status == EventStatus.Published ? "Event published successfully." : 
                    status == EventStatus.Draft ? "Event unpublished successfully." : 
                    $"Event status updated to {status}.");
            }
            catch (Exception ex)
            {
                return ApiResponse<bool>.ErrorResult($"Error updating event status: {ex.Message}");
            }
        }

        #endregion
    }
}