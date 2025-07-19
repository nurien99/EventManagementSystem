using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventManagementSystem.Core.DTOs.Email;

namespace EventManagementSystem.Core
{
    public class EmailOutbox
    {
        [Key]
        public int EmailID { get; set; } // Consistent with your naming pattern

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string ToEmail { get; set; }

        [StringLength(500)]
        public string CcEmails { get; set; } = string.Empty;

        [StringLength(500)]
        public string BccEmails { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; } // Store as HTML

        public bool IsHtml { get; set; } = true;

        public EmailType Type { get; set; }

        public EmailStatus Status { get; set; } = EmailStatus.Pending;

        public int? RelatedEntityId { get; set; } // Foreign key to Event, User, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? SentAt { get; set; }

        public DateTime? FailedAt { get; set; }

        [StringLength(1000)]
        public string ErrorMessage { get; set; } = string.Empty;

        public int RetryCount { get; set; } = 0;

        public int MaxRetries { get; set; } = 3;

        public DateTime? NextRetryAt { get; set; }

        // Serialized attachment data (JSON)
        public string AttachmentsJson { get; set; } = string.Empty;

        // Computed properties
        [NotMapped]
        public bool CanRetry => (Status == EmailStatus.Pending || Status == EmailStatus.Failed) &&
                               RetryCount < MaxRetries &&
                               (NextRetryAt == null || NextRetryAt <= DateTime.UtcNow);

        // Helper method (no NotMapped needed for methods)
        public TimeSpan GetRetryDelay()
        {
            // Exponential backoff: 1min, 5min, 30min
            return RetryCount switch
            {
                0 => TimeSpan.FromMinutes(1),
                1 => TimeSpan.FromMinutes(5),
                2 => TimeSpan.FromMinutes(30),
                _ => TimeSpan.FromHours(1)
            };
        }
    }
}