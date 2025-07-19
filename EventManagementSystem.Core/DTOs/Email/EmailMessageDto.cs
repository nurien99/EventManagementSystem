using EventManagementSystem.Core;

namespace EventManagementSystem.Core.DTOs.Email
{
    public class EmailMessageDto
    {
        public string To { get; set; } = string.Empty;
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public Core.EmailType Type { get; set; } = Core.EmailType.SystemNotification; // ✅ Fully qualified
        public int? RelatedEntityId { get; set; }
        public List<EmailAttachmentDto> Attachments { get; set; } = new List<EmailAttachmentDto>();
    }

    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}