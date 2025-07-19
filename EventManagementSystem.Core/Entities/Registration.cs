
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Core
{
    public class Registration
    {
        [Key]
        public int RegisterID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int EventID { get; set; }

        // Registration status
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Confirmed;

        // Registration timestamp
        public DateTime? RegisteredAt { get; set; }

        // Cancellation fields
        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string CancellationReason { get; set; } = string.Empty;

        // Attendee-specific information (for guest registrations or override user info)
        [StringLength(100)]
        public string AttendeeName { get; set; }

        [EmailAddress]
        [StringLength(255)]
        public string AttendeeEmail { get; set; }

        [StringLength(20)]
        public string AttendeePhone { get; set; }

        [StringLength(200)]
        public string AttendeeOrganization { get; set; }

        // Special requirements
        [StringLength(500)]
        public string SpecialRequirements { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("EventID")]
        public virtual Event Event { get; set; }

        public virtual ICollection<IssuedTicket> IssuedTickets { get; set; } = new List<IssuedTicket>();

        // Computed properties
        [NotMapped]
        public bool IsActive => Status == RegistrationStatus.Confirmed || Status == RegistrationStatus.CheckedIn;

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(AttendeeName) ? AttendeeName : User?.Name;

        [NotMapped]
        public string DisplayEmail => !string.IsNullOrEmpty(AttendeeEmail) ? AttendeeEmail : User?.Email;
    }
}
