
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.DTOs
{
    public class RegistrationDto
    {
        public int RegisterID { get; set; }
        public int UserID { get; set; }
        public int EventID { get; set; }
        public string EventName { get; set; }
        public RegistrationStatus Status { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public string AttendeeName { get; set; }
        public string AttendeeEmail { get; set; }
        public string AttendeePhone { get; set; }
        public string AttendeeOrganization { get; set; }
        public string SpecialRequirements { get; set; }
        public List<IssuedTicketDto> IssuedTickets { get; set; } = new List<IssuedTicketDto>();
    }
}
