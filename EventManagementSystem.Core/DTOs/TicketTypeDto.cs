using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.DTOs
{
    public class TicketTypeDto
    {
        public int TicketTypeID { get; set; }
        public int EventID { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int SoldQuantity { get; set; }
        public int AvailableQuantity => Quantity - SoldQuantity;
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsSaleActive => IsActive &&
            (SaleStartDate == null || SaleStartDate <= DateTime.Now) &&
            (SaleEndDate == null || SaleEndDate >= DateTime.Now);
    }
}
