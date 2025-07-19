using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string Email { get; set; }
    }

    // Response DTO
    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        // Note: Never return the actual token in response for security
    }
}
