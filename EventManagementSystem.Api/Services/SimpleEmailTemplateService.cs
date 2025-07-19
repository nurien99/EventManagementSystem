// File: Services/SimpleEmailTemplateService.cs
using EventManagementSystem.Core.DTOs.Email;
using EventManagementSystem.Api.Services.Interfaces;

namespace EventManagementSystem.Api.Services
{
    public class SimpleEmailTemplateService : IEmailTemplateService
    {
        private readonly ILogger<SimpleEmailTemplateService> _logger;

        public SimpleEmailTemplateService(ILogger<SimpleEmailTemplateService> logger)
        {
            _logger = logger;
        }

        // ✅ PUBLIC METHOD - Interface implementation
        public Task<string> RenderTemplateAsync<T>(string templateName, T model) where T : BaseEmailModel
        {
            try
            {
                _logger.LogInformation("🎨 Rendering simple template: {TemplateName}", templateName);

                string html = templateName switch
                {
                    "RegistrationConfirmation" => RenderRegistrationConfirmation((RegistrationConfirmationModel)(object)model),
                    "UserWelcome" => RenderUserWelcome((UserWelcomeModel)(object)model),
                    "EventReminder" => RenderEventReminder((EventReminderModel)(object)model),
                    "EventCancellation" => RenderEventCancellation((EventCancellationModel)(object)model),
                    "PasswordReset" => RenderPasswordReset((PasswordResetModel)(object)model),
                    _ => throw new ArgumentException($"Template '{templateName}' not found")
                };

                _logger.LogInformation("✅ Simple template rendered successfully. Length: {Length}", html.Length);
                return Task.FromResult(html);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error rendering simple template {TemplateName}", templateName);
                throw;
            }
        }

        public Task<bool> ValidateTemplateAsync(string templateName)
        {
            var availableTemplates = GetAvailableTemplates();
            var isValid = availableTemplates.Contains(templateName);
            return Task.FromResult(isValid);
        }

        public List<string> GetAvailableTemplates()
        {
            return new List<string>
            {
                "RegistrationConfirmation",
                "UserWelcome",
                "EventReminder",
                "EventCancellation",
                "PasswordReset"
            };
        }

        // ✅ PRIVATE METHOD - Helper method (should be INSIDE the class, not inside another method)
        private string RenderRegistrationConfirmation(RegistrationConfirmationModel model)
        {
            // Debug info
            var qrCodeLength = model.QRCodeBase64?.Length ?? 0;
            var qrCodeEmpty = string.IsNullOrEmpty(model.QRCodeBase64);
            var qrCodePreview = model.QRCodeBase64?.Length > 50
                ? model.QRCodeBase64.Substring(0, 50) + "..."
                : model.QRCodeBase64 ?? "NULL";

            // QR Code section - Back to Base64
            var qrCodeSection = !string.IsNullOrEmpty(model.QRCodeBase64)
                ? $@"
                <div style='text-align: center; margin: 20px 0; padding: 20px; background-color: white; border-radius: 10px; border: 2px solid #dee2e6;'>
                    <h3>📱 Your Digital Ticket</h3>
                    <div style='margin: 15px 0;'>
                        <img src='data:image/png;base64,{model.QRCodeBase64}' 
                             alt='Ticket QR Code' 
                             style='display: block; margin: 0 auto; width: 200px; height: 200px; border: 1px solid #ccc; border-radius: 5px;' />
                    </div>
                    <p><strong>📲 Important:</strong> Present this QR code at the event for check-in</p>
                    <p><small>💾 Save this email or take a screenshot of the QR code</small></p>
                </div>"
                : @"
                <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                    <h4>⚠️ QR Code Not Available</h4>
                    <p>Your digital ticket QR code could not be generated. Please contact support with your ticket number: <strong>" + model.TicketNumber + @"</strong></p>
                </div>";

            // Ticket types section
            var ticketTypesSection = "";
            if (model.TicketTypes != null && model.TicketTypes.Count > 0)
            {
                var ticketItems = "";
                foreach (var ticket in model.TicketTypes)
                {
                    var priceText = ticket.Price > 0 ? $"(${ticket.Price:F2} each)" : "(Free)";
                    ticketItems += $"<p>• {ticket.Quantity} x {ticket.TypeName} {priceText}</p>";
                }

                ticketTypesSection = $@"
                <div style='background-color: #e8f4f8; padding: 15px; border-radius: 10px; margin: 20px 0;'>
                    <h4>🎟️ Your Tickets:</h4>
                    {ticketItems}
                </div>";
            }

            // End time section
            var endTimeSection = "";
            if (model.EventEndDate.HasValue)
            {
                endTimeSection = $"<p><strong>🕐 End Time:</strong> {model.EventEndDate.Value:h:mm tt}</p>";
            }

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Registration Confirmed - {model.EventTitle}</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; }}
        .header {{ background: linear-gradient(135deg, #2ecc71 0%, #27ae60 100%); color: white; padding: 30px; text-align: center; }}
        .content {{ padding: 30px; }}
        .ticket-info {{ background: #f8f9fa; border: 2px dashed #007bff; padding: 20px; margin: 20px 0; border-radius: 10px; }}
        .footer {{ background-color: #2c3e50; color: white; padding: 20px; text-align: center; font-size: 12px; }}
        .debug {{ background: #fff3e0; padding: 15px; margin: 10px 0; border: 2px solid #ff9800; border-radius: 5px; font-family: monospace; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Registration Confirmed!</h1>
            <p>You're all set for {model.EventTitle}</p>
        </div>

        <div class='content'>
            <h2>Hello {model.UserName},</h2>

            <!-- DEBUG SECTION -->
            <div class='debug'>
                <h4 style='color: #e65100; margin-top: 0;'>🔍 DEBUG INFO</h4>
                <p><strong>QRCodeBase64 Length:</strong> {qrCodeLength}</p>
                <p><strong>QRCodeBase64 Empty:</strong> {qrCodeEmpty}</p>
                <p><strong>UserName:</strong> {model.UserName}</p>
                <p><strong>Email:</strong> {model.Email}</p>
                <p><strong>TicketNumber:</strong> {model.TicketNumber}</p>
                <p><strong>EventTitle:</strong> {model.EventTitle}</p>
                {(qrCodeEmpty ? "<p style='color: red; font-weight: bold;'>❌ QR CODE DATA IS MISSING!</p>" : "<p style='color: green; font-weight: bold;'>✅ QR CODE DATA IS PRESENT!</p>")}
            </div>

            <p>Great news! Your registration for <strong>{model.EventTitle}</strong> has been confirmed.</p>

            <div class='ticket-info'>
                <h3>🎫 Your Event Details</h3>
                <p><strong>📅 Event:</strong> {model.EventTitle}</p>
                <p><strong>🕐 Date & Time:</strong> {model.EventStartDate:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                {endTimeSection}
                <p><strong>📍 Venue:</strong> {model.VenueName}</p>
                <p><strong>🗺️ Address:</strong> {model.VenueAddress}</p>
                <p><strong>🎫 Ticket Number:</strong> <span style='font-family: monospace; background-color: #e9ecef; padding: 5px; border-radius: 3px;'>#{model.TicketNumber}</span></p>
            </div>

            {qrCodeSection}

            {ticketTypesSection}

            <div style='background-color: #d4edda; padding: 15px; border-radius: 10px; margin: 20px 0;'>
                <h4>✅ What to Expect:</h4>
                <ul>
                    <li>📧 You'll receive a reminder email 24 hours before the event</li>
                    <li>🚪 Arrive 15 minutes early for smooth check-in</li>
                    <li>📱 Have your QR code ready on your phone or printed</li>
                    <li>🆔 Bring a valid ID if required</li>
                </ul>
            </div>

            <div style='text-align: center; margin: 30px 0;'>
                <p>Need to cancel your registration?</p>
                <a href='{model.CancellationUrl}' style='color: #dc3545; text-decoration: none;'>❌ Cancel Registration</a>
            </div>

            <p>We're excited to see you at the event! If you have any questions, please don't hesitate to contact us.</p>
        </div>

        <div class='footer'>
            <p>&copy; {DateTime.Now.Year} {model.SiteName ?? "Event Management System"}. All rights reserved.</p>
            <p>Registration ID: {model.RegistrationId}</p>
        </div>
    </div>
</body>
</html>";
        }

        // ✅ OTHER PRIVATE HELPER METHODS
        private string RenderUserWelcome(UserWelcomeModel model)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Welcome to {model.SiteName}</title>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f4f4f4;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: white;'>
        <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center;'>
            <h1>Welcome, {model.UserName}!</h1>
        </div>
        <div style='padding: 30px;'>
            <p>Welcome to our platform!</p>
        </div>
    </div>
</body>
</html>";
        }

        private string RenderEventReminder(EventReminderModel model)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Event Reminder</title>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h1>Event Reminder</h1>
    <p>Hello {model.UserName}, your event {model.EventTitle} is coming up!</p>
</body>
</html>";
        }

        private string RenderEventCancellation(EventCancellationModel model)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Event Cancelled</title>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h1>Event Cancelled</h1>
    <p>Hello {model.UserName}, unfortunately {model.EventTitle} has been cancelled.</p>
</body>
</html>";
        }

        private string RenderPasswordReset(PasswordResetModel model)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Password Reset</title>
</head>
<body style='font-family: Arial, sans-serif;'>
    <h1>Password Reset</h1>
    <p>Hello {model.UserName}, click the link to reset your password.</p>
</body>
</html>";
        }

    } // ✅ END OF CLASS
}