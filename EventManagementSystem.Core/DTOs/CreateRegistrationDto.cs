using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.DTOs
{
    public class CreateRegistrationDto
    {
        [Required]
        public int EventID { get; set; }

        public int? UserID { get; set; } // Optional for guest registration

        [Required]
        [StringLength(100)]
        public string AttendeeName { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string AttendeeEmail { get; set; }

        [StringLength(20)]
        public string AttendeePhone { get; set; }

        [StringLength(200)]
        public string AttendeeOrganization { get; set; }

        [StringLength(500)]
        public string SpecialRequirements { get; set; }

        [Required]
        public List<TicketSelectionDto> TicketSelections { get; set; } = new List<TicketSelectionDto>();
    }

    public class TicketSelectionDto
    {
        public int TicketTypeID { get; set; }
        public int Quantity { get; set; }
    }

}
