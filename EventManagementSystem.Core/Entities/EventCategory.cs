using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Core
{
    [Index(nameof(CategoryName), IsUnique = true)] // Index defined at CLASS level
    public class EventCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } // NO [Index] here

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string IconClass { get; set; }

        [StringLength(20)]
        public string ColorCode { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}