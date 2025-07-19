using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagementSystem.Core
{
    public class EventAssistant
    {
        [Key]
        public int EventAssistantID { get; set; }

        [Required]
        public int EventID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public AssistantRole Role { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public int AssignedByUserID { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("EventID")]
        public virtual Event Event { get; set; }

        [ForeignKey("UserID")]
        public virtual User Assistant { get; set; }

        [ForeignKey("AssignedByUserID")]
        public virtual User AssignedBy { get; set; }
    }

}