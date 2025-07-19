using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManagementSystem.Core.Configuration
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public bool EnableEmailSending { get; set; } = true;
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMinutes { get; set; } = 5;
        public int BatchSize { get; set; } = 10;
    }

    public class SiteSettings
    {
        public string SiteName { get; set; } = "Event Management System";
        public string SiteUrl { get; set; } = "https://localhost:7203";
        public string SupportEmail { get; set; } = "support@eventmanagement.com";
        public string NoReplyEmail { get; set; } = "noreply@eventmanagement.com";
    }
}