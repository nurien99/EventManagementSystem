using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Core
{
    [Index(nameof(UniqueReferenceCode), IsUnique = true)] // Index defined at CLASS level
    public class IssuedTicket
    {
        [Key]
        public int IssuedTicketID { get; set; }

        [Required]
        public int RegisterID { get; set; }

        [Required]
        public int TicketTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string UniqueReferenceCode { get; set; } // NO [Index] here

        [StringLength(1000)]
        public string QRCodeData { get; set; }

        [Required]
        [StringLength(100)]
        public string AttendeeName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string AttendeeEmail { get; set; }

        [StringLength(20)]
        public string AttendeePhoneNo { get; set; }

        [StringLength(200)]
        public string AttendeeOrganization { get; set; }

        public DateTime? CheckedInAt { get; set; }
        public int? CheckedInByUserID { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Valid;

        public DateTime? IssuedAt { get; set; }

        // Navigation properties
        public virtual Registration Registration { get; set; }
        public virtual TicketType TicketType { get; set; }
        public virtual User CheckedInByUser { get; set; }
    }
}