
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.DTOs
{
    public class IssuedTicketDto
    {
        public int IssuedTicketID { get; set; }
        public string UniqueReferenceCode { get; set; }
        public string QRCodeData { get; set; }
        public string TicketTypeName { get; set; }
        public string AttendeeName { get; set; }
        public string AttendeeEmail { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public TicketStatus Status { get; set; }
        public DateTime? IssuedAt { get; set; }
    }

}
