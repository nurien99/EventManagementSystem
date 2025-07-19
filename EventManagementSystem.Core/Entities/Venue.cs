using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Core
{
    public class Venue
    {
        [Key]
        public int VenueID { get; set; }

        [Required]
        [StringLength(200)]
        public string VenueName { get; set; }

        [Required]
        [StringLength(500)]
        public string Address { get; set; }

        // Additional venue details
        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [StringLength(100)]
        public string Country { get; set; }

        // Venue capacity
        [Range(1, int.MaxValue)]
        public int? Capacity { get; set; }

        // Contact information
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [StringLength(500)]
        public string Website { get; set; }

        // Facilities and amenities
        [StringLength(1000)]
        public string Facilities { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        // GPS coordinates
        [Column(TypeName = "decimal(10, 8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11, 8)")]
        public decimal? Longitude { get; set; }

        // Venue status
        public bool IsActive { get; set; } = true;

        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();

        // Computed properties
        [NotMapped]
        public string FullAddress => $"{Address}, {City}, {State} {PostalCode}, {Country}".Trim();

        [NotMapped]
        public bool HasCoordinates => Latitude.HasValue && Longitude.HasValue;
    }
}
