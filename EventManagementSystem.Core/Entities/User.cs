using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EventManagementSystem.Core
{
    [Index(nameof(Email), IsUnique = true)] // Index defined at CLASS level
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } // NO [Index] here

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        public UserRole Role { get; set; } = UserRole.Attendee;

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Organization { get; set; }

        public bool IsEmailVerified { get; set; } = false;
        public string EmailVerificationToken { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public string PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }

        // Navigation properties
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}