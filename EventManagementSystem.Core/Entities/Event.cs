using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Core
{
    [Index(nameof(UrlSlug), IsUnique = true)] // Index defined at CLASS level
    public class Event
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        [StringLength(200)]
        public string EventName { get; set; }

        [StringLength(2000)]
        public string EventDesc { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int VenueID { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Draft;

        public int? MaxCapacity { get; set; }

        public DateTime? RegistrationDeadline { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public int? CategoryID { get; set; }

        [StringLength(250)]
        public string UrlSlug { get; set; } // NO [Index] here

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User Organizer { get; set; }
        public virtual Venue Venue { get; set; }
        public virtual EventCategory Category { get; set; }
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<TicketType> TicketTypes { get; set; } = new List<TicketType>();
    }
}