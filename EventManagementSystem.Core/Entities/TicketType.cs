using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Core
{
    public class TicketType
    {
        [Key]
        public int TicketTypeID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Track sold tickets
        public int SoldQuantity { get; set; } = 0;

        // Sale period
        public DateTime? SaleStartDate { get; set; }
        public DateTime? SaleEndDate { get; set; }

        // Ticket type status
        public bool IsActive { get; set; } = true;

        // Display order for multiple ticket types
        public int DisplayOrder { get; set; } = 0;

        // Timestamps
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("EventID")]
        public virtual Event Event { get; set; }

        public virtual ICollection<IssuedTicket> IssuedTickets { get; set; } = new List<IssuedTicket>();

        // Computed properties
        [NotMapped]
        public int AvailableQuantity => Math.Max(0, Quantity - SoldQuantity);

        [NotMapped]
        public bool IsSaleActive => IsActive &&
                                   (SaleStartDate == null || SaleStartDate <= DateTime.Now) &&
                                   (SaleEndDate == null || SaleEndDate >= DateTime.Now) &&
                                   AvailableQuantity > 0;

        [NotMapped]
        public bool IsFree => Price == 0;
    }
}