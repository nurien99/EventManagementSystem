using EventManagementSystem.Api.Data;
using EventManagementSystem.Core;
using EventManagementSystem.Core.Configuration;
using EventManagementSystem.Core.DTOs.Email;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Json;

namespace EventManagementSystem.Api.Services
{
    public class HangfireEmailJobs
    {
        /// <summary>
        /// Background job method that processes emails using MailKit with enhanced logging
        /// </summary>
        public static async Task ProcessEmailJobAsync(int emailId)
        {
            ILogger<HangfireEmailJobs>? logger = null;

            try
            {
                // Use service locator pattern to get dependencies
                using var scope = ServiceProviderAccessor.ServiceProvider?.CreateScope();
                if (scope == null)
                {
                    Console.WriteLine("❌ Service provider not available");
                    return;
                }

                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var emailSettings = scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value;
                logger = scope.ServiceProvider.GetRequiredService<ILogger<HangfireEmailJobs>>();

                logger.LogInformation("🚀 Starting email processing job for ID: {EmailId}", emailId);

                var outboxEntry = await context.EmailOutbox.FindAsync(emailId);
                if (outboxEntry == null)
                {
                    logger.LogWarning("❌ Email outbox entry not found for ID {EmailId}", emailId);
                    return;
                }

                logger.LogInformation("📧 Found email: To={ToEmail}, Subject={Subject}, Status={Status}",
                    outboxEntry.ToEmail, outboxEntry.Subject, outboxEntry.Status);

                // Check if email should be processed
                if (outboxEntry.Status != EmailStatus.Pending && outboxEntry.Status != EmailStatus.Failed)
                {
                    logger.LogInformation("⏭️ Email {EmailId} already processed with status {Status}", emailId, outboxEntry.Status);
                    return;
                }

                if (!outboxEntry.CanRetry)
                {
                    logger.LogWarning("🛑 Email {EmailId} cannot be retried. Max retries exceeded.", emailId);
                    return;
                }

                // Check if email sending is enabled
                if (!emailSettings.EnableEmailSending)
                {
                    logger.LogWarning("📵 Email sending is disabled in configuration");
                    return;
                }

                try
                {
                    logger.LogInformation("📤 Attempting to send email {EmailId} using MailKit...", emailId);

                    // Send email using MailKit
                    await SendEmailWithMailKitAsync(outboxEntry, emailSettings, logger);

                    // Mark as sent
                    outboxEntry.Status = EmailStatus.Sent;
                    outboxEntry.SentAt = DateTime.UtcNow;
                    outboxEntry.ErrorMessage = string.Empty;

                    logger.LogInformation("✅ Successfully sent email {EmailId} to {Recipient}", emailId, outboxEntry.ToEmail);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Failed to send email {EmailId}: {Message}", emailId, ex.Message);

                    // Handle failure with retry logic
                    await HandleEmailFailureAsync(outboxEntry, ex.Message, logger);
                }

                await context.SaveChangesAsync();
                logger.LogInformation("💾 Database updated for email {EmailId}", emailId);
            }
            catch (Exception ex)
            {
                var errorMsg = $"💥 Critical error in email job {emailId}: {ex.Message}";
                Console.WriteLine(errorMsg);
                logger?.LogError(ex, errorMsg);
            }
        }

        /// <summary>
        /// Send email using MailKit with enhanced logging and error handling
        /// </summary>
        private static async Task SendEmailWithMailKitAsync(EmailOutbox outboxEntry, EmailSettings emailSettings, ILogger logger)
        {
            logger.LogInformation("🔨 Building email message for {EmailId}", outboxEntry.EmailID);

            var message = new MimeMessage();

            // Set sender
            message.From.Add(new MailboxAddress(emailSettings.SenderName, emailSettings.SenderEmail));
            logger.LogDebug("📨 Sender set: {SenderName} <{SenderEmail}>", emailSettings.SenderName, emailSettings.SenderEmail);

            // Set recipient
            message.To.Add(MailboxAddress.Parse(outboxEntry.ToEmail));
            logger.LogDebug("📧 Recipient set: {Recipient}", outboxEntry.ToEmail);

            // Add CC recipients
            if (!string.IsNullOrEmpty(outboxEntry.CcEmails))
            {
                foreach (var cc in outboxEntry.CcEmails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    message.Cc.Add(MailboxAddress.Parse(cc.Trim()));
                    logger.LogDebug("📧 CC added: {CC}", cc.Trim());
                }
            }

            // Add BCC recipients
            if (!string.IsNullOrEmpty(outboxEntry.BccEmails))
            {
                foreach (var bcc in outboxEntry.BccEmails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    message.Bcc.Add(MailboxAddress.Parse(bcc.Trim()));
                    logger.LogDebug("📧 BCC added: {BCC}", bcc.Trim());
                }
            }

            // Set subject
            message.Subject = outboxEntry.Subject;
            logger.LogDebug("📝 Subject set: {Subject}", outboxEntry.Subject);

            // Build message body
            var bodyBuilder = new BodyBuilder();
            if (outboxEntry.IsHtml)
            {
                bodyBuilder.HtmlBody = outboxEntry.Body;
                logger.LogDebug("🌐 HTML body set (length: {Length} chars)", outboxEntry.Body.Length);
            }
            else
            {
                bodyBuilder.TextBody = outboxEntry.Body;
                logger.LogDebug("📄 Text body set (length: {Length} chars)", outboxEntry.Body.Length);
            }

            // Handle attachments
            if (!string.IsNullOrEmpty(outboxEntry.AttachmentsJson))
            {
                try
                {
                    var attachments = JsonSerializer.Deserialize<List<EmailAttachmentData>>(outboxEntry.AttachmentsJson);
                    if (attachments != null)
                    {
                        foreach (var attachment in attachments)
                        {
                            bodyBuilder.Attachments.Add(
                                attachment.FileName,
                                attachment.Content,
                                ContentType.Parse(attachment.ContentType)
                            );
                            logger.LogDebug("📎 Attachment added: {FileName} ({ContentType})", attachment.FileName, attachment.ContentType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "⚠️ Error processing attachments for email {EmailId}", outboxEntry.EmailID);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            // Send using MailKit SMTP
            using var client = new SmtpClient();
            try
            {
                logger.LogInformation("🔗 Connecting to SMTP server: {Server}:{Port}", emailSettings.SmtpServer, emailSettings.Port);

                // Connect to SMTP server
                await client.ConnectAsync(emailSettings.SmtpServer, emailSettings.Port, SecureSocketOptions.StartTls);
                logger.LogInformation("✅ Connected to SMTP server successfully");

                // Authenticate
                logger.LogInformation("🔐 Authenticating with SMTP server using username: {Username}", emailSettings.Username);
                await client.AuthenticateAsync(emailSettings.Username, emailSettings.Password);
                logger.LogInformation("✅ SMTP authentication successful");

                // Send message
                logger.LogInformation("📤 Sending message...");
                var messageId = await client.SendAsync(message);
                logger.LogInformation("✅ Message sent successfully! Message ID: {MessageId}", messageId);

                logger.LogInformation("📧 MailKit successfully sent email {EmailId} to {Recipient}", outboxEntry.EmailID, outboxEntry.ToEmail);
            }
            catch (AuthenticationException authEx)
            {
                logger.LogError(authEx, "🔐 SMTP Authentication failed for email {EmailId}", outboxEntry.EmailID);
                throw new Exception($"SMTP Authentication failed: {authEx.Message}", authEx);
            }
            catch (SmtpCommandException smtpEx)
            {
                logger.LogError(smtpEx, "📧 SMTP Command failed for email {EmailId}: {StatusCode} - {Message}",
                    outboxEntry.EmailID, smtpEx.StatusCode, smtpEx.Message);
                throw new Exception($"SMTP Command failed: {smtpEx.StatusCode} - {smtpEx.Message}", smtpEx);
            }
            catch (SmtpProtocolException protocolEx)
            {
                logger.LogError(protocolEx, "🔗 SMTP Protocol error for email {EmailId}: {Message}",
                    outboxEntry.EmailID, protocolEx.Message);
                throw new Exception($"SMTP Protocol error: {protocolEx.Message}", protocolEx);
            }
            finally
            {
                if (client.IsConnected)
                {
                    logger.LogDebug("🔚 Disconnecting from SMTP server");
                    await client.DisconnectAsync(true);
                }
            }
        }

        /// <summary>
        /// Handle email sending failures with exponential backoff
        /// </summary>
        private static async Task HandleEmailFailureAsync(EmailOutbox outboxEntry, string errorMessage, ILogger logger)
        {
            outboxEntry.Status = EmailStatus.Failed;
            outboxEntry.FailedAt = DateTime.UtcNow;
            outboxEntry.ErrorMessage = errorMessage;
            outboxEntry.RetryCount++;

            if (outboxEntry.RetryCount < outboxEntry.MaxRetries)
            {
                // Schedule retry with exponential backoff
                outboxEntry.NextRetryAt = DateTime.UtcNow.Add(outboxEntry.GetRetryDelay());

                logger.LogWarning("🔄 Email {EmailId} failed (attempt {RetryCount}/{MaxRetries}), will retry at {NextRetry}. Error: {Error}",
                    outboxEntry.EmailID, outboxEntry.RetryCount, outboxEntry.MaxRetries, outboxEntry.NextRetryAt, errorMessage);

                // Schedule retry job
                using var scope = ServiceProviderAccessor.ServiceProvider?.CreateScope();
                if (scope != null)
                {
                    var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
                    backgroundJobClient.Schedule(() => ProcessEmailJobAsync(outboxEntry.EmailID), outboxEntry.NextRetryAt.Value);
                    logger.LogInformation("📅 Retry job scheduled for email {EmailId} at {NextRetry}", outboxEntry.EmailID, outboxEntry.NextRetryAt);
                }
            }
            else
            {
                logger.LogError("💀 Email {EmailId} failed permanently after {RetryCount} attempts. Final error: {Error}",
                    outboxEntry.EmailID, outboxEntry.RetryCount, errorMessage);
            }
        }
    }

    /// <summary>
    /// Simple email attachment data for JSON serialization
    /// </summary>
    public class EmailAttachmentData
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }

    /// <summary>
    /// Service provider accessor for static Hangfire jobs
    /// </summary>
    public static class ServiceProviderAccessor
    {
        public static IServiceProvider? ServiceProvider { get; set; }
    }
}