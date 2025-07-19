using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class CreateEventDto
    {
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(200, ErrorMessage = "Event name cannot exceed 200 characters")]
        public string EventName { get; set; }

        [StringLength(2000, ErrorMessage = "Event description cannot exceed 2000 characters")]
        public string EventDesc { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; } // Made optional with nullable

        // For selecting an EXISTING venue
        public int? VenueID { get; set; }

        // For creating a NEW venue
        [StringLength(200)]
        public string? NewVenueName { get; set; }

        [StringLength(500)]
        public string? NewVenueAddress { get; set; }

        // Additional venue details for new venue
        [StringLength(100)]
        public string? NewVenueCity { get; set; }

        [StringLength(100)]
        public string? NewVenueState { get; set; }

        [StringLength(20)]
        public string? NewVenuePostalCode { get; set; }

        // Event enhancements
        public int? MaxCapacity { get; set; }

        public DateTime? RegistrationDeadline { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; }

        public int? CategoryID { get; set; }

        // Ticket types for the event
        public List<CreateTicketTypeDto> TicketTypes { get; set; } = new List<CreateTicketTypeDto>();

        // Custom validation method
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            // Must have either existing venue or new venue details
            if (!VenueID.HasValue && (string.IsNullOrWhiteSpace(NewVenueName) || string.IsNullOrWhiteSpace(NewVenueAddress)))
            {
                errors.Add("Either select an existing venue or provide new venue details");
            }

            // End date must be after start date
            if (EndDate.HasValue && EndDate <= StartDate)
            {
                errors.Add("End date must be after start date");
            }

            // Registration deadline must be before start date
            if (RegistrationDeadline.HasValue && RegistrationDeadline >= StartDate)
            {
                errors.Add("Registration deadline must be before event start date");
            }

            return errors.Count == 0;
        }
    }
}