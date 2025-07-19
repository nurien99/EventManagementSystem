using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Core.DTOs
{
    public class EventAssistantDto
    {
        public int EventAssistantID { get; set; }
        public int EventID { get; set; }
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public AssistantRole Role { get; set; }
        public DateTime AssignedAt { get; set; }
        public string AssignedByUserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class AssignAssistantDto
    {
        [Required]
        public int EventID { get; set; }

        [Required]
        [EmailAddress]
        public string AssistantEmail { get; set; } = string.Empty;

        [Required]
        public AssistantRole Role { get; set; }
    }

    public class UpdateAssistantRoleDto
    {
        [Required]
        public AssistantRole Role { get; set; }
    }
}