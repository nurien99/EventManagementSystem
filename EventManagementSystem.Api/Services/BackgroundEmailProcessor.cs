// EventManagementSystem.Api/Services/BackgroundEmailProcessor.cs
using EventManagementSystem.Api.Data;
using EventManagementSystem.Core;
using EventManagementSystem.Core.Configuration;
using EventManagementSystem.Core.DTOs.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using FluentEmail.Core;
using System.Text.Json;

namespace EventManagementSystem.Api.Services
{
    public class BackgroundEmailProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailSettings _emailSettings;
        private readonly IFluentEmailFactory _fluentEmailFactory;
        private readonly ILogger<BackgroundEmailProcessor> _logger;

        public BackgroundEmailProcessor(
            ApplicationDbContext context,
            IOptions<EmailSettings> emailSettings,
            IFluentEmailFactory fluentEmailFactory,
            ILogger<BackgroundEmailProcessor> logger)
        {
            _context = context;
            _emailSettings = emailSettings.Value;
            _fluentEmailFactory = fluentEmailFactory;
            _logger = logger;
        }

        public async Task ProcessEmailAsync(int emailId)
        {
            try
            {
                _logger.LogInformation("Starting to process email {EmailId}", emailId);

                var outboxEntry = await _context.EmailOutbox.FindAsync(emailId);
                if (outboxEntry == null)
                {
                    _logger.LogWarning("Email outbox entry not found for ID {EmailId}", emailId);
                    return;
                }

                if (outboxEntry.Status != EmailStatus.Pending && outboxEntry.Status != EmailStatus.Failed)
                {
                    _logger.LogInformation("Email {EmailId} already processed with status {Status}", emailId, outboxEntry.Status);
                    return;
                }

                if (!outboxEntry.CanRetry)
                {
                    _logger.LogWarning("Email {EmailId} cannot be retried. Max retries exceeded.", emailId);
                    return;
                }

                try
                {
                    // Send email using FluentEmail
                    await SendEmailWithFluentEmailAsync(outboxEntry);

                    // Mark as sent
                    outboxEntry.Status = EmailStatus.Sent;
                    outboxEntry.SentAt = DateTime.UtcNow;
                    outboxEntry.ErrorMessage = string.Empty;

                    _logger.LogInformation("Successfully processed email {EmailId} for {Recipient}", emailId, outboxEntry.ToEmail);
                }
                catch (Exception ex)
                {
                    await HandleEmailFailureAsync(outboxEntry, ex.Message);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background email processor for ID {EmailId}", emailId);
            }
        }

        private async Task SendEmailWithFluentEmailAsync(EmailOutbox outboxEntry)
        {
            var email = _fluentEmailFactory
                .Create()
                .To(outboxEntry.ToEmail)
                .Subject(outboxEntry.Subject);

            // Add CC and BCC if provided
            if (!string.IsNullOrEmpty(outboxEntry.CcEmails))
            {
                foreach (var cc in outboxEntry.CcEmails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    email = email.CC(cc.Trim());
                }
            }

            if (!string.IsNullOrEmpty(outboxEntry.BccEmails))
            {
                foreach (var bcc in outboxEntry.BccEmails.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    email = email.BCC(bcc.Trim());
                }
            }

            // Set body
            if (outboxEntry.IsHtml)
                email = email.Body(outboxEntry.Body, true);
            else
                email = email.Body(outboxEntry.Body);

            // Handle attachments if any
            if (!string.IsNullOrEmpty(outboxEntry.AttachmentsJson))
            {
                try
                {
                    var attachments = JsonSerializer.Deserialize<List<EmailAttachmentDto>>(outboxEntry.AttachmentsJson);
                    if (attachments != null)
                    {
                        foreach (var attachment in attachments)
                        {
                            email = email.Attach(new FluentEmail.Core.Models.Attachment
                            {
                                Data = new MemoryStream(attachment.Content),
                                Filename = attachment.FileName,
                                ContentType = attachment.ContentType
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing attachments for email {EmailId}", outboxEntry.EmailID);
                }
            }

            var result = await email.SendAsync();

            if (!result.Successful)
            {
                var errorMessage = string.Join(", ", result.ErrorMessages);
                throw new Exception($"FluentEmail failed: {errorMessage}");
            }

            _logger.LogInformation("Email sent successfully via FluentEmail to {Recipient}", outboxEntry.ToEmail);
        }

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
    }
}