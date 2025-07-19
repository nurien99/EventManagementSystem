using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.Configuration;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core.DTOs.Email;
using FluentEmail.Core;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text.Json;

namespace EventManagementSystem.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailTemplateService _templateService;
        private readonly IFluentEmail _fluentEmail;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly EmailSettings _emailSettings;
        private readonly SiteSettings _siteSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(
            ApplicationDbContext context,
            IEmailTemplateService templateService,
            IFluentEmail fluentEmail,
            IBackgroundJobClient backgroundJobClient,
            IOptions<EmailSettings> emailSettings,
            IOptions<SiteSettings> siteSettings,
            ILogger<EmailService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _templateService = templateService;
            _fluentEmail = fluentEmail;
            _backgroundJobClient = backgroundJobClient;
            _emailSettings = emailSettings.Value;
            _siteSettings = siteSettings.Value;
            _logger = logger;
            _configuration = configuration;
        }

        // Keep your existing password reset method for backward compatibility
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Reset Your Password";

                email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = $"<p>Please reset your password by clicking here: <a href='{resetLink}'>Reset Password</a></p>"
                };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["Port"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
                throw;
            }
        }

        // New enhanced email methods
        public async Task<ApiResponse<bool>> SendEmailAsync(EmailMessageDto emailMessage)
        {
            try
            {
                if (!_emailSettings.EnableEmailSending)
                {
                    _logger.LogWarning("Email sending is disabled in configuration");
                    return ApiResponse<bool>.SuccessResult(false, "Email sending is disabled");
                }

                var email = _fluentEmail
                    .To(emailMessage.To)
                    .Subject(emailMessage.Subject);

                if (!string.IsNullOrEmpty(emailMessage.Cc))
                    email = email.CC(emailMessage.Cc);

                if (!string.IsNullOrEmpty(emailMessage.Bcc))
                    email = email.BCC(emailMessage.Bcc);

                if (emailMessage.IsHtml)
                    email = email.Body(emailMessage.Body, true);
                else
                    email = email.Body(emailMessage.Body);

                // Add attachments if any
                foreach (var attachment in emailMessage.Attachments)
                {
                    email = email.Attach(new FluentEmail.Core.Models.Attachment
                    {
                        Data = new MemoryStream(attachment.Content),
                        Filename = attachment.FileName,
                        ContentType = attachment.ContentType
                    });
                }

                var result = await email.SendAsync();

                if (result.Successful)
                {
                    _logger.LogInformation("Email sent successfully to {Recipient}", emailMessage.To);
                    return ApiResponse<bool>.SuccessResult(true, "Email sent successfully");
                }
                else
                {
                    var errorMessage = string.Join(", ", result.ErrorMessages);
                    _logger.LogError("Failed to send email to {Recipient}: {Error}", emailMessage.To, errorMessage);
                    return ApiResponse<bool>.ErrorResult($"Failed to send email: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Recipient}", emailMessage.To);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending email", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> QueueEmailAsync(EmailMessageDto emailMessage)
        {
            try
            {
                // Store email in outbox (Outbox Pattern)
                var outboxEntry = new EmailOutbox
                {
                    ToEmail = emailMessage.To,
                    CcEmails = emailMessage.Cc ?? string.Empty,
                    BccEmails = emailMessage.Bcc ?? string.Empty,
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    IsHtml = emailMessage.IsHtml,
                    Type = emailMessage.Type,
                    Status = EmailStatus.Pending,
                    RelatedEntityId = emailMessage.RelatedEntityId,
                    CreatedAt = DateTime.UtcNow,
                    AttachmentsJson = emailMessage.Attachments.Any()
                        ? JsonSerializer.Serialize(emailMessage.Attachments.Select(a => new EmailAttachmentData
                        {
                            FileName = a.FileName,
                            Content = a.Content,
                            ContentType = a.ContentType
                        }))
                        : string.Empty,
                    MaxRetries = _emailSettings.MaxRetries
                };

                _context.EmailOutbox.Add(outboxEntry);
                await _context.SaveChangesAsync();

                // Queue for background processing with Hangfire
                _backgroundJobClient.Enqueue(() => HangfireEmailJobs.ProcessEmailJobAsync(outboxEntry.EmailID));

                _logger.LogInformation("📤 Email queued successfully for {Recipient} with ID {EmailId}",
                    emailMessage.To, outboxEntry.EmailID);

                return ApiResponse<bool>.SuccessResult(true, "Email queued successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error queuing email for {Recipient}", emailMessage.To);
                return ApiResponse<bool>.ErrorResult("An error occurred while queuing email", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> SendTemplateEmailAsync<T>(string toEmail, string subject, string templateName, T model) where T : BaseEmailModel
        {
            try
            {
                // Populate base model properties
                model.SiteName = _siteSettings.SiteName;
                model.SiteUrl = _siteSettings.SiteUrl;
                model.SentAt = DateTime.UtcNow;

                // Render template
                var htmlContent = await _templateService.RenderTemplateAsync(templateName, model);

                var emailMessage = new EmailMessageDto
                {
                    To = toEmail,
                    Subject = subject,
                    Body = htmlContent,
                    IsHtml = true,
                    Type = DetermineEmailType(templateName),
                    RelatedEntityId = ExtractEntityId(model)
                };

                // Queue the email for background processing
                return await QueueEmailAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending template email {TemplateName} to {Recipient}", templateName, toEmail);
                return ApiResponse<bool>.ErrorResult("An error occurred while sending template email", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<bool>> ProcessPendingEmailsAsync()
        {
            try
            {
                var pendingEmails = await _context.EmailOutbox
                    .Where(e => (e.Status == EmailStatus.Pending ||
                                (e.Status == EmailStatus.Failed && e.CanRetry)) &&
                               (e.NextRetryAt == null || e.NextRetryAt <= DateTime.UtcNow))
                    .Take(_emailSettings.BatchSize)
                    .ToListAsync();

                _logger.LogInformation("🔄 Processing {Count} pending emails", pendingEmails.Count);

                foreach (var email in pendingEmails)
                {
                    _backgroundJobClient.Enqueue(() => HangfireEmailJobs.ProcessEmailJobAsync(email.EmailID));
                }

                return ApiResponse<bool>.SuccessResult(true, $"Queued {pendingEmails.Count} emails for processing");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing pending emails");
                return ApiResponse<bool>.ErrorResult("An error occurred while processing pending emails", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<List<EmailOutbox>>> GetEmailHistoryAsync(int? relatedEntityId = null, EmailType? type = null)
        {
            try
            {
                var query = _context.EmailOutbox.AsQueryable();

                if (relatedEntityId.HasValue)
                    query = query.Where(e => e.RelatedEntityId == relatedEntityId.Value);

                if (type.HasValue)
                    query = query.Where(e => e.Type == type.Value);

                var emails = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(100) // Limit to recent 100 emails
                    .ToListAsync();

                return ApiResponse<List<EmailOutbox>>.SuccessResult(emails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email history");
                return ApiResponse<List<EmailOutbox>>.ErrorResult("An error occurred while retrieving email history", new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<EmailOutbox>> GetEmailByIdAsync(int emailId)
        {
            try
            {
                var email = await _context.EmailOutbox.FindAsync(emailId);
                if (email == null)
                {
                    return ApiResponse<EmailOutbox>.ErrorResult("Email not found");
                }

                return ApiResponse<EmailOutbox>.SuccessResult(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email {EmailId}", emailId);
                return ApiResponse<EmailOutbox>.ErrorResult("An error occurred while retrieving email", new List<string> { ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task HandleEmailFailureAsync(EmailOutbox outboxEntry, string errorMessage)
        {
            outboxEntry.Status = EmailStatus.Failed;
            outboxEntry.FailedAt = DateTime.UtcNow;
            outboxEntry.ErrorMessage = errorMessage;
            outboxEntry.RetryCount++;

            if (outboxEntry.RetryCount < outboxEntry.MaxRetries)
            {
                // Schedule retry with exponential backoff
                outboxEntry.NextRetryAt = DateTime.UtcNow.Add(outboxEntry.GetRetryDelay());

                _logger.LogWarning("Email {EmailId} failed, scheduling retry {RetryCount}/{MaxRetries} at {NextRetry}",
                    outboxEntry.EmailID, outboxEntry.RetryCount, outboxEntry.MaxRetries, outboxEntry.NextRetryAt);
            }
            else
            {
                _logger.LogError("Email {EmailId} failed permanently after {RetryCount} attempts: {Error}",
                    outboxEntry.EmailID, outboxEntry.RetryCount, errorMessage);
            }
        }

        private EmailType DetermineEmailType(string templateName)
        {
            return templateName.ToLower() switch
            {
                "registrationconfirmation" => EmailType.EventRegistrationConfirmation,
                "eventreminder" => EmailType.EventReminder,
                "eventcancellation" => EmailType.EventCancellation,
                "userwelcome" => EmailType.UserRegistration,
                "passwordreset" => EmailType.PasswordReset,
                "emailverification" => EmailType.EmailVerification,
                _ => EmailType.UserRegistration
            };
        }

        private int? ExtractEntityId<T>(T model) where T : BaseEmailModel
        {
            return model switch
            {
                RegistrationConfirmationModel regModel when int.TryParse(regModel.RegistrationId, out var regId) => regId,
                _ => null
            };
        }

        #endregion
    }
}