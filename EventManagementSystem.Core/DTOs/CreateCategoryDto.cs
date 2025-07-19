using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string CategoryName { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string Description { get; set; }

        [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters")]
        public string IconClass { get; set; }

        [StringLength(20, ErrorMessage = "Color code cannot exceed 20 characters")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Color code must be a valid hex color (e.g., #FF5733)")]
        public string ColorCode { get; set; }
    }
}