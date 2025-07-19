using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Password and confirmation do not match")]
        public string ConfirmPassword { get; set; }

        // Additional profile information
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(200)]
        public string Organization { get; set; }

        // Optional: User role (default to Attendee if not specified)
        public UserRole Role { get; set; } = UserRole.Attendee;
    }

    // User profile DTO for response
        public class UserDto
        {
            public int UserID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public UserRole Role { get; set; }
            public string? PhoneNumber { get; set; }
            public string? Organization { get; set; }
            public bool IsEmailVerified { get; set; }
            public bool IsActive { get; set; }
            public DateTime? CreatedAt { get; set; }
        }
    }
