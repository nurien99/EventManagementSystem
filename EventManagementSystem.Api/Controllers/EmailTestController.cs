using EventManagementSystem.Api.Data;
using EventManagementSystem.Api.Services;
using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core;
using EventManagementSystem.Core.Configuration;
using EventManagementSystem.Core.DTOs;
using EventManagementSystem.Core.DTOs.Email;
using FluentEmail.Core;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System.ComponentModel.DataAnnotations;

namespace EventManagementSystem.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly IQRCodeService _qrCodeService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailTestController> _logger;

        public EmailTestController(
            IEmailService emailService,
            IQRCodeService qrCodeService,
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailTestController> logger)
        {
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Test basic email sending functionality
        /// </summary>
        [HttpPost("send-test-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> SendTestEmail([FromBody] TestEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.ErrorResult("Validation failed.", errors));
            }

            var emailMessage = new EmailMessageDto
            {
                To = request.ToEmail,
                Subject = "Test Email from Event Management System",
                Body = $@"
                    <h2>🎉 Email System Test</h2>
                    <p>Hello!</p>
                    <p>This is a test email from your Event Management System.</p>
                    <p><strong>Sent at:</strong> {DateTime.Now:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                    <p>If you received this email, your email system is working correctly!</p>
                    <hr>
                    <p><small>Event Management System - Powered by ASP.NET Core</small></p>
                ",
                IsHtml = true,
                Type = EmailType.UserRegistration
            };

            var result = await _emailService.QueueEmailAsync(emailMessage);
            return Ok(result);
        }

        /// <summary>
        /// 🔧 Test raw SMTP connection without any frameworks
        /// </summary>
        [HttpPost("test-raw-smtp")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<string>>> TestRawSmtp([FromBody] TestEmailRequest request)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(MailboxAddress.Parse(request.ToEmail));
                message.Subject = "🔧 RAW SMTP Test - Event Management System";

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <h2>🔧 Raw SMTP Connection Test</h2>
                        <p>This email was sent directly using MailKit SMTP without any background jobs or queuing.</p>
                        <p><strong>Sent at:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                        <p><strong>SMTP Server:</strong> {_emailSettings.SmtpServer}:{_emailSettings.Port}</p>
                        <p><strong>Sender:</strong> {_emailSettings.SenderEmail}</p>
                        <p>If you see this, your SMTP configuration is working!</p>
                    "
                };

                using var client = new SmtpClient();

                _logger.LogInformation("🔗 Connecting to SMTP server: {Server}:{Port}", _emailSettings.SmtpServer, _emailSettings.Port);
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);

                _logger.LogInformation("🔐 Authenticating with username: {Username}", _emailSettings.Username);
                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);

                _logger.LogInformation("📤 Sending email to: {Recipient}", request.ToEmail);
                var result = await client.SendAsync(message);

                await client.DisconnectAsync(true);

                _logger.LogInformation("✅ Raw SMTP test successful! Message ID: {MessageId}", result);

                return Ok(ApiResponse<string>.SuccessResult(
                    $"Raw SMTP test successful! Message sent at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    "Email sent successfully via raw SMTP"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Raw SMTP test failed");
                return BadRequest(ApiResponse<string>.ErrorResult($"Raw SMTP test failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 📊 Get email configuration diagnostics
        /// </summary>
        [HttpGet("diagnostics")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> GetEmailDiagnostics()
        {
            var diagnostics = new
            {
                EmailEnabled = _emailSettings.EnableEmailSending,
                SmtpServer = _emailSettings.SmtpServer,
                Port = _emailSettings.Port,
                UseSsl = _emailSettings.UseSsl,
                SenderEmail = _emailSettings.SenderEmail,
                SenderName = _emailSettings.SenderName,
                MaxRetries = _emailSettings.MaxRetries,
                BatchSize = _emailSettings.BatchSize,
                Username = !string.IsNullOrEmpty(_emailSettings.Username) ? "✅ Set" : "❌ Missing",
                Password = !string.IsNullOrEmpty(_emailSettings.Password) ? "✅ Set" : "❌ Missing",
                CurrentTime = DateTime.Now,
                ServerTimeZone = TimeZoneInfo.Local.DisplayName
            };

            return Ok(ApiResponse<object>.SuccessResult(diagnostics));
        }

        /// <summary>
        /// 📧 Test FluentEmail with detailed logging
        /// </summary>
        [HttpPost("test-fluent-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> TestFluentEmailDetailed([FromBody] TestEmailRequest request)
        {
            try
            {
                var fluentEmailFactory = HttpContext.RequestServices.GetRequiredService<IFluentEmailFactory>();

                _logger.LogInformation("🔨 Creating FluentEmail instance");
                var email = fluentEmailFactory
                    .Create()
                    .To(request.ToEmail)
                    .Subject("🔨 FluentEmail Test")
                    .Body($@"
                        <h2>🔨 FluentEmail Test</h2>
                        <p>This email was sent using FluentEmail with detailed logging.</p>
                        <p><strong>Test Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                        <p><strong>Sender:</strong> {_emailSettings.SenderEmail}</p>
                        <p><strong>Recipient:</strong> {request.ToEmail}</p>
                        <p>Check the API logs for detailed information about the sending process.</p>
                    ", true);

                _logger.LogInformation("📤 Sending email via FluentEmail to: {Recipient}", request.ToEmail);
                var result = await email.SendAsync();

                var response = new
                {
                    Successful = result.Successful,
                    ErrorMessages = result.ErrorMessages?.ToList() ?? new List<string>(),
                    MessageId = result.MessageId,
                    SentAt = DateTime.Now
                };

                if (result.Successful)
                {
                    _logger.LogInformation("✅ FluentEmail test successful!");
                    return Ok(ApiResponse<object>.SuccessResult(response, "FluentEmail sent successfully"));
                }
                else
                {
                    var errors = string.Join(", ", result.ErrorMessages);
                    _logger.LogError("❌ FluentEmail test failed: {Errors}", errors);
                    return BadRequest(ApiResponse<object>.ErrorResult($"FluentEmail failed: {errors}", result.ErrorMessages?.ToList()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ FluentEmail detailed test exception");
                return BadRequest(ApiResponse<object>.ErrorResult($"FluentEmail test exception: {ex.Message}"));
            }
        }

        /// <summary>
        /// 📋 Check email outbox status
        /// </summary>
        [HttpGet("outbox-status")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> GetOutboxStatus()
        {
            try
            {
                var emailHistory = await _emailService.GetEmailHistoryAsync();

                if (!emailHistory.Success)
                    return BadRequest(emailHistory);

                var recentEmails = emailHistory.Data.Take(10).ToList();

                var status = new
                {
                    TotalEmails = emailHistory.Data.Count,
                    PendingCount = emailHistory.Data.Count(e => e.Status == EmailStatus.Pending),
                    SentCount = emailHistory.Data.Count(e => e.Status == EmailStatus.Sent),
                    FailedCount = emailHistory.Data.Count(e => e.Status == EmailStatus.Failed),
                    RecentEmails = recentEmails.Select(e => new
                    {
                        e.EmailID,
                        e.ToEmail,
                        e.Subject,
                        e.Status,
                        e.CreatedAt,
                        e.SentAt,
                        e.FailedAt,
                        e.ErrorMessage,
                        e.RetryCount,
                        e.NextRetryAt
                    }).ToList()
                };

                return Ok(ApiResponse<object>.SuccessResult(status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting outbox status");
                return BadRequest(ApiResponse<object>.ErrorResult($"Error getting outbox status: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🔄 Manually process pending emails (for debugging)
        /// </summary>
        [HttpPost("process-pending-manually")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> ProcessPendingEmailsManually()
        {
            try
            {
                var emailHistory = await _emailService.GetEmailHistoryAsync();
                if (!emailHistory.Success)
                    return BadRequest(emailHistory);

                var pendingEmails = emailHistory.Data
                    .Where(e => e.Status == EmailStatus.Pending)
                    .OrderBy(e => e.CreatedAt)
                    .Take(5)
                    .ToList();

                var results = new List<object>();

                foreach (var email in pendingEmails)
                {
                    try
                    {
                        _logger.LogInformation("🔧 Manually processing email {EmailId}", email.EmailID);

                        // Try to process the email using the background processor directly
                        var backgroundProcessor = HttpContext.RequestServices.GetRequiredService<BackgroundEmailProcessor>();
                        await backgroundProcessor.ProcessEmailAsync(email.EmailID);

                        results.Add(new
                        {
                            EmailId = email.EmailID,
                            Status = "✅ Processed",
                            ToEmail = email.ToEmail,
                            Subject = email.Subject
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Failed to process email {EmailId}", email.EmailID);
                        results.Add(new
                        {
                            EmailId = email.EmailID,
                            Status = "❌ Failed",
                            Error = ex.Message,
                            ToEmail = email.ToEmail
                        });
                    }
                }

                return Ok(ApiResponse<object>.SuccessResult(new
                {
                    ProcessedCount = results.Count,
                    Results = results
                }, "Manual processing completed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in manual email processing");
                return BadRequest(ApiResponse<object>.ErrorResult($"Manual processing failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🚀 Force trigger Hangfire job for specific email
        /// </summary>
        [HttpPost("trigger-hangfire-job/{emailId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<string>>> TriggerHangfireJob(int emailId)
        {
            try
            {
                var backgroundJobClient = HttpContext.RequestServices.GetRequiredService<IBackgroundJobClient>();

                // Queue the job immediately
                var jobId = backgroundJobClient.Enqueue(() => HangfireEmailJobs.ProcessEmailJobAsync(emailId));

                _logger.LogInformation("🚀 Triggered Hangfire job {JobId} for email {EmailId}", jobId, emailId);

                return Ok(ApiResponse<string>.SuccessResult(jobId, $"Hangfire job triggered for email {emailId}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to trigger Hangfire job for email {EmailId}", emailId);
                return BadRequest(ApiResponse<string>.ErrorResult($"Failed to trigger job: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🧹 Clear all failed Hangfire jobs using correct API
        /// </summary>
        [HttpPost("clear-failed-jobs")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<string>> ClearFailedJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();

                // Get failed jobs using the monitoring API
                var failedJobs = monitoringApi.FailedJobs(0, 100);

                var deletedCount = 0;
                foreach (var job in failedJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    deletedCount++;
                }

                _logger.LogInformation("🧹 Cleared {Count} failed Hangfire jobs", deletedCount);

                return Ok(ApiResponse<string>.SuccessResult(
                    $"Cleared {deletedCount} failed jobs",
                    "Failed jobs cleared successfully"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing failed jobs");
                return BadRequest(ApiResponse<string>.ErrorResult($"Error clearing jobs: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🧹 Clear ALL Hangfire jobs using correct API
        /// </summary>
        [HttpPost("clear-all-jobs")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<string>> ClearAllJobs()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var totalDeleted = 0;

                // Clear failed jobs
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                foreach (var job in failedJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    totalDeleted++;
                }

                // Clear enqueued jobs
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                foreach (var job in enqueuedJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    totalDeleted++;
                }

                // Clear scheduled jobs
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                foreach (var job in scheduledJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    totalDeleted++;
                }

                // Clear processing jobs
                var processingJobs = monitoringApi.ProcessingJobs(0, 1000);
                foreach (var job in processingJobs)
                {
                    BackgroundJob.Delete(job.Key);
                    totalDeleted++;
                }

                _logger.LogInformation("🧹 Cleared {Count} total Hangfire jobs", totalDeleted);

                return Ok(ApiResponse<string>.SuccessResult(
                    $"Cleared {totalDeleted} total jobs",
                    "All jobs cleared successfully - fresh start!"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error clearing all jobs");
                return BadRequest(ApiResponse<string>.ErrorResult($"Error clearing jobs: {ex.Message}"));
            }
        }

        /// <summary>
        /// 💥 Nuclear option: Reset Hangfire tables (Development only!)
        /// </summary>
        [HttpPost("nuclear-reset")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<string>>> NuclearReset()
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                // Execute raw SQL to clear Hangfire tables
                await context.Database.ExecuteSqlRawAsync("DELETE FROM HangFire.Job");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM HangFire.JobParameter");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM HangFire.JobQueue");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM HangFire.State");

                _logger.LogInformation("💥 Nuclear reset completed - all Hangfire jobs cleared");

                return Ok(ApiResponse<string>.SuccessResult(
                    "Nuclear reset completed",
                    "All Hangfire jobs and states cleared from database"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in nuclear reset");
                return BadRequest(ApiResponse<string>.ErrorResult($"Nuclear reset failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// 📊 Get Hangfire job statistics using correct API
        /// </summary>
        [HttpGet("hangfire-stats")]
        [AllowAnonymous]
        public ActionResult<ApiResponse<object>> GetHangfireStats()
        {
            try
            {
                var monitoringApi = JobStorage.Current.GetMonitoringApi();

                var stats = new
                {
                    FailedJobs = monitoringApi.FailedCount(),
                    EnqueuedJobs = monitoringApi.EnqueuedCount("default"),
                    ProcessingJobs = monitoringApi.ProcessingCount(),
                    ScheduledJobs = monitoringApi.ScheduledCount(),
                    SucceededJobs = monitoringApi.SucceededListCount(),
                    Servers = monitoringApi.Servers().Count,
                    Queues = monitoringApi.Queues().Select(q => new {
                        Name = q.Name,
                        Length = q.Length
                    }).ToList()
                };

                return Ok(ApiResponse<object>.SuccessResult(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting Hangfire stats");
                return BadRequest(ApiResponse<object>.ErrorResult($"Error getting stats: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🔄 Reset all pending emails to fresh state
        /// </summary>
        [HttpPost("reset-pending-emails")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> ResetPendingEmails()
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                var pendingEmails = await context.EmailOutbox
                    .Where(e => e.Status == EmailStatus.Pending)
                    .ToListAsync();

                foreach (var email in pendingEmails)
                {
                    email.RetryCount = 0;
                    email.ErrorMessage = string.Empty;
                    email.FailedAt = null;
                    email.NextRetryAt = null;
                    email.Status = EmailStatus.Pending;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("🔄 Reset {Count} pending emails to fresh state", pendingEmails.Count);

                return Ok(ApiResponse<object>.SuccessResult(new
                {
                    ResetCount = pendingEmails.Count,
                    Message = "All pending emails reset to fresh state"
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error resetting pending emails");
                return BadRequest(ApiResponse<object>.ErrorResult($"Error resetting emails: {ex.Message}"));
            }
        }

        /// <summary>
        /// 🚀 Test fresh email with clean Hangfire job
        /// </summary>
        [HttpPost("test-fresh-email")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> TestFreshEmail([FromBody] TestEmailRequest request)
        {
            try
            {
                var emailMessage = new EmailMessageDto
                {
                    To = request.ToEmail,
                    Subject = "✨ Fresh Test Email - Hangfire Fixed!",
                    Body = $@"
                        <h2>✨ Fresh Email Test</h2>
                        <p>This email was sent after fixing the Hangfire job issue!</p>
                        <p><strong>Sent at:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                        <p>If you receive this, the background job system is working correctly!</p>
                    ",
                    IsHtml = true,
                    Type = EmailType.SystemNotification
                };

                var queueResult = await _emailService.QueueEmailAsync(emailMessage);

                if (!queueResult.Success)
                {
                    return BadRequest(queueResult);
                }

                await Task.Delay(2000);

                var historyResult = await _emailService.GetEmailHistoryAsync();
                var latestEmail = historyResult.Data?.OrderByDescending(e => e.CreatedAt).FirstOrDefault();

                return Ok(ApiResponse<object>.SuccessResult(new
                {
                    EmailQueued = queueResult.Success,
                    LatestEmailStatus = latestEmail?.Status.ToString(),
                    LatestEmailId = latestEmail?.EmailID,
                    Message = "Fresh email test completed - check your inbox!"
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in fresh email test");
                return BadRequest(ApiResponse<object>.ErrorResult($"Fresh email test failed: {ex.Message}"));
            }
        }

        /// <summary>
        /// Test template email sending
        /// </summary>
        [HttpPost("send-template-test")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<bool>>> SendTemplateTest([FromBody] TestTemplateEmailRequest request)
        {
            var welcomeModel = new UserWelcomeModel
            {
                UserName = request.UserName,
                Email = request.ToEmail,
                EmailVerificationUrl = "https://localhost:7203/verify-email?token=test-token",
                LoginUrl = "https://localhost:7203/login"
            };

            var result = await _emailService.SendTemplateEmailAsync(
                request.ToEmail,
                "Welcome to Event Management System!",
                "UserWelcome",
                welcomeModel
            );

            return Ok(result);
        }

        /// <summary>
        /// Test QR Code generation
        /// </summary>
        [HttpPost("generate-qr-test")]
        [AllowAnonymous]
        public async Task<ActionResult> GenerateQRTest([FromBody] TestQRRequest request)
        {
            try
            {
                var qrCodeBytes = await _qrCodeService.GenerateTicketQRCodeAsync(request.Data);
                return File(qrCodeBytes, "image/png", "test-qr-code.png");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating QR code: {ex.Message}");
            }
        }

        /// <summary>
        /// Get email history for testing
        /// </summary>
        [HttpGet("email-history")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<EmailOutbox>>>> GetEmailHistory()
        {
            var result = await _emailService.GetEmailHistoryAsync();
            return Ok(result);
        }


        // Add this test endpoint to your EmailTestController.cs

        /// <summary>
        /// Debug QR code generation for a specific registration
        /// </summary>
        [HttpGet("debug-qr/{registrationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugQRCodeGeneration(int registrationId)
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                // Get registration with all related data
                var registration = await context.Registrations
                    .Include(r => r.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(r => r.IssuedTickets)
                        .ThenInclude(it => it.TicketType)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

                if (registration == null)
                {
                    return NotFound($"Registration {registrationId} not found");
                }

                var debugInfo = new
                {
                    RegistrationId = registration.RegisterID,
                    AttendeeName = registration.AttendeeName,
                    AttendeeEmail = registration.AttendeeEmail,
                    EventName = registration.Event?.EventName,
                    VenueName = registration.Event?.Venue?.VenueName,
                    IssuedTicketsCount = registration.IssuedTickets?.Count ?? 0,
                    PrimaryTicket = registration.IssuedTickets?.FirstOrDefault() != null ? new
                    {
                        TicketId = registration.IssuedTickets.First().IssuedTicketID,
                        UniqueReferenceCode = registration.IssuedTickets.First().UniqueReferenceCode,
                        TicketTypeId = registration.IssuedTickets.First().TicketTypeID,
                        TicketTypeName = registration.IssuedTickets.First().TicketType?.TypeName
                    } : null
                };

                string qrCodeBase64 = "";
                string qrError = "";

                // Test QR code generation
                if (registration.IssuedTickets?.Any() == true)
                {
                    var primaryTicket = registration.IssuedTickets.First();
                    try
                    {
                        var secureData = await _qrCodeService.GenerateSecureTicketDataAsync(
                            registration.RegisterID,
                            primaryTicket.TicketTypeID,
                            registration.AttendeeEmail
                        );

                        var qrBytes = await _qrCodeService.GenerateTicketQRCodeAsync(secureData);
                        qrCodeBase64 = Convert.ToBase64String(qrBytes);
                    }
                    catch (Exception ex)
                    {
                        qrError = ex.Message;
                    }
                }

                // Create HTML response
                var html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <title>QR Debug - Registration {registrationId}</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; line-height: 1.6; }}
                .section {{ background: #f9f9f9; padding: 15px; margin: 10px 0; border-radius: 5px; }}
                .error {{ background: #ffebee; border-left: 4px solid #f44336; }}
                .success {{ background: #e8f5e8; border-left: 4px solid #4caf50; }}
                .debug {{ background: #fff3e0; border-left: 4px solid #ff9800; }}
                pre {{ background: #f5f5f5; padding: 10px; border-radius: 3px; overflow-x: auto; }}
                .qr-code {{ text-align: center; padding: 20px; }}
            </style>
        </head>
        <body>
            <h1>🔍 QR Code Debug - Registration {registrationId}</h1>
            
            <div class='section debug'>
                <h3>📋 Registration Debug Info</h3>
                <pre>{System.Text.Json.JsonSerializer.Serialize(debugInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}</pre>
            </div>

            <div class='section {(string.IsNullOrEmpty(qrError) ? "success" : "error")}'>
                <h3>🔄 QR Code Generation</h3>
                <p><strong>QR Code Base64 Length:</strong> {qrCodeBase64.Length}</p>
                <p><strong>QR Code Empty:</strong> {string.IsNullOrEmpty(qrCodeBase64)}</p>
                {(!string.IsNullOrEmpty(qrError) ? $"<p><strong>❌ Error:</strong> {qrError}</p>" : "")}
                {(!string.IsNullOrEmpty(qrCodeBase64) ? $"<p><strong>✅ Preview:</strong> {qrCodeBase64.Substring(0, Math.Min(100, qrCodeBase64.Length))}...</p>" : "")}
            </div>

            {(!string.IsNullOrEmpty(qrCodeBase64) ? $@"
            <div class='section success'>
                <div class='qr-code'>
                    <h3>📱 Generated QR Code</h3>
                    <img src='data:image/png;base64,{qrCodeBase64}' alt='QR Code' style='width:200px;height:200px;border:2px solid #ccc;border-radius:10px;' />
                    <p><small>This is exactly what will appear in your email</small></p>
                </div>
            </div>" : @"
            <div class='section error'>
                <h3>❌ QR Code Generation Failed</h3>
                <p>No QR code was generated. Check the error message above.</p>
            </div>")}

            <div class='section'>
                <h3>🧪 Next Steps</h3>
                <p>1. If QR code appears above → <a href='/api/emailtest/test-registration-email/{registrationId}'>Test Full Registration Email</a></p>
                <p>2. If QR code is missing → Check the error message and logs</p>
                <p>3. Check email outbox → <a href='/api/emailtest/outbox-status'>View Email Status</a></p>
            </div>
        </body>
        </html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Test sending registration confirmation email for a specific registration
        /// </summary>
        [HttpPost("test-registration-email/{registrationId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> TestRegistrationEmail(int registrationId)
        {
            try
            {
                var notificationService = HttpContext.RequestServices.GetRequiredService<INotificationService>();

                _logger.LogInformation("🧪 Testing registration confirmation email for registration {RegistrationId}", registrationId);

                var result = await notificationService.SendRegistrationConfirmationAsync(registrationId);

                if (result.Success)
                {
                    return Ok(new ApiResponse<object>
                    {
                        Success = true,
                        Message = "✅ Registration confirmation email sent successfully!",
                        Data = new
                        {
                            RegistrationId = registrationId,
                            SentAt = DateTime.Now,
                            Instructions = "Check your email inbox and spam folder"
                        }
                    });
                }
                else
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = result.Message,
                        Errors = result.Errors,
                        Data = new { RegistrationId = registrationId }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error testing registration email for registration {RegistrationId}", registrationId);
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = new { RegistrationId = registrationId }
                });
            }
        }


        // Add this test endpoint to your EmailTestController.cs

        // Update your QR code endpoint in EmailTestController.cs

        /// <summary>
        /// Get QR code image with proper headers for email display
        /// </summary>
        [HttpGet("qr-code-image/{registrationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQRCodeImage(int registrationId)
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                var registration = await context.Registrations
                    .Include(r => r.IssuedTickets)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

                if (registration == null)
                    return NotFound("Registration not found");

                var primaryTicket = registration.IssuedTickets.FirstOrDefault();
                if (primaryTicket == null)
                    return NotFound("No tickets found");

                // Generate QR code
                var qrCodeBase64 = await _qrCodeService.GenerateTicketQRCodeBase64Async(
                    registrationId,
                    primaryTicket.TicketTypeID,
                    registration.AttendeeEmail
                );

                // Convert Base64 to bytes
                var qrCodeBytes = Convert.FromBase64String(qrCodeBase64);

                // Set proper headers for email client compatibility
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("Access-Control-Allow-Methods", "GET");
                Response.Headers.Add("Cache-Control", "public, max-age=3600"); // Cache for 1 hour
                Response.Headers.Add("Content-Disposition", $"inline; filename=ticket-{registrationId}.png");

                // Return as image with proper content type
                return File(qrCodeBytes, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image for registration {RegistrationId}", registrationId);

                // Return a placeholder image on error
                var placeholderBytes = CreatePlaceholderQRImage($"Error: {ex.Message}");
                return File(placeholderBytes, "image/png");
            }
        }

        private byte[] CreatePlaceholderQRImage(string errorMessage)
        {
            // Create a simple error placeholder image
            var width = 200;
            var height = 200;

            using var bitmap = new System.Drawing.Bitmap(width, height);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);

            graphics.Clear(System.Drawing.Color.White);
            graphics.FillRectangle(System.Drawing.Brushes.LightGray, 0, 0, width, height);

            var font = new System.Drawing.Font("Arial", 10);
            var brush = System.Drawing.Brushes.Red;
            var text = "QR Code\nError";

            graphics.DrawString(text, font, brush, 10, 10);

            using var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            return stream.ToArray();
        }

        /// <summary>
        /// Test QR code in simple HTML page
        /// </summary>
        [HttpGet("qr-code-test-page/{registrationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetQRCodeTestPage(int registrationId)
        {
            try
            {
                var context = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();

                var registration = await context.Registrations
                    .Include(r => r.IssuedTickets)
                    .FirstOrDefaultAsync(r => r.RegisterID == registrationId);

                if (registration == null)
                    return NotFound("Registration not found");

                var primaryTicket = registration.IssuedTickets.FirstOrDefault();
                if (primaryTicket == null)
                    return NotFound("No tickets found");

                // Generate QR code
                var qrCodeBase64 = await _qrCodeService.GenerateTicketQRCodeBase64Async(
                    registrationId,
                    primaryTicket.TicketTypeID,
                    registration.AttendeeEmail
                );

                var html = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <title>QR Code Test - Registration {registrationId}</title>
                        <style>
                            body {{ font-family: Arial, sans-serif; margin: 40px; text-align: center; }}
                            .qr-container {{ border: 2px solid #ccc; padding: 20px; margin: 20px auto; max-width: 400px; }}
                            .debug {{ background: #f0f0f0; padding: 15px; margin: 20px 0; text-align: left; }}
                        </style>
                    </head>
                    <body>
                        <h1>QR Code Test for Registration {registrationId}</h1>
    
                        <div class='debug'>
                            <h3>Debug Information:</h3>
                            <p><strong>Registration ID:</strong> {registrationId}</p>
                            <p><strong>Ticket ID:</strong> {primaryTicket.IssuedTicketID}</p>
                            <p><strong>QR Code Length:</strong> {qrCodeBase64.Length}</p>
                            <p><strong>QR Code Preview:</strong> {qrCodeBase64.Substring(0, Math.Min(100, qrCodeBase64.Length))}...</p>
                        </div>

                        <div class='qr-container'>
                            <h3>Method 1: Inline Base64</h3>
                            <img src='data:image/png;base64,{qrCodeBase64}' 
                                 alt='QR Code Method 1' 
                                 style='max-width: 200px; border: 1px solid #ddd;' />
                        </div>

                        <div class='qr-container'>
                            <h3>Method 2: Direct Image URL</h3>
                            <img src='/api/emailtest/qr-code-image/{registrationId}' 
                                 alt='QR Code Method 2' 
                                 style='max-width: 200px; border: 1px solid #ddd;' />
                        </div>

                        <p>If both QR codes appear correctly above, then the QR generation is working perfectly!</p>
                        <p>The issue might be with email client compatibility with Base64 images.</p>
    
                        <div style='margin: 30px 0;'>
                            <a href='/api/emailtest/qr-code-image/{registrationId}' download='ticket-qr-{registrationId}.png'>
                                📥 Download QR Code as PNG
                            </a>
                        </div>
                    </body>
                    </html>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }

    // Request models for testing
    public class TestEmailRequest
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; }
    }

    public class TestTemplateEmailRequest
    {
        [Required]
        [EmailAddress]
        public string ToEmail { get; set; }

        [Required]
        public string UserName { get; set; }
    }

    public class TestQRRequest
    {
        [Required]
        public string Data { get; set; }
    }
}