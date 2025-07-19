using System;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class EventFilterDto
    {
        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Search and filters
        public string SearchTerm { get; set; } = string.Empty;
        public int? CategoryID { get; set; }
        public int? VenueID { get; set; }
        public EventStatus? Status { get; set; }

        // Date filters
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }

        // Location filter
        public string City { get; set; } = string.Empty;

        // Organizer filter
        public int? OrganizerID { get; set; }

        // Sorting
        public string SortBy { get; set; } = "StartDate"; // StartDate, EventName, CreatedAt
        public string SortDirection { get; set; } = "asc"; // asc, desc

        // Additional filters
        public bool? IsFree { get; set; } // Filter for free events
        public bool? HasAvailableSpots { get; set; } // Filter for events with available spots
    }
}