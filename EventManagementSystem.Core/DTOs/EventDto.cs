
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.DTOs
{
    public class EventDto
    {
        public int EventID { get; set; }
        public string EventName { get; set; }
        public string EventDesc { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EventStatus Status { get; set; }
        public int? MaxCapacity { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public string ImageUrl { get; set; }
        public string UrlSlug { get; set; }

        // Organizer details
        public int UserID { get; set; }
        public string OrganizerName { get; set; }

        // Venue details
        public int VenueID { get; set; }
        public string VenueName { get; set; }
        public string VenueAddress { get; set; }

        // Category details
        public int? CategoryID { get; set; }
        public string CategoryName { get; set; }

        // Statistics
        public int TotalRegistrations { get; set; }
        public int AvailableSpots { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<TicketTypeDto> TicketTypes { get; set; } = new List<TicketTypeDto>();
    }
}