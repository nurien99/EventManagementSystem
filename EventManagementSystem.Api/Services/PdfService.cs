using EventManagementSystem.Api.Services.Interfaces;
using EventManagementSystem.Core.DTOs;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using System.Text;

namespace EventManagementSystem.Api.Services;

public class PdfService : IPdfService
{
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<PdfService> _logger;

    public PdfService(IQRCodeService qrCodeService, ILogger<PdfService> logger)
    {
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<byte[]> GenerateTicketPdfAsync(UserTicketDto ticket)
    {
        try
        {
            _logger.LogInformation("🎟️ Generating PDF ticket for: {EventName}", ticket.EventName);

            // Generate QR code as Base64
            var qrCodeBase64 = await _qrCodeService.GenerateTicketQRCodeBase64Async(ticket.QRCodeData);

            // Create HTML template
            var htmlTemplate = GenerateTicketHtml(ticket, qrCodeBase64);

            // Convert HTML to PDF
            using var pdfStream = new MemoryStream();
            var converterProperties = new ConverterProperties();
            converterProperties.SetBaseUri(".");
            
            HtmlConverter.ConvertToPdf(htmlTemplate, pdfStream, converterProperties);
            
            _logger.LogInformation("✅ PDF ticket generated successfully for: {EventName}", ticket.EventName);
            return pdfStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generating PDF ticket for: {EventName}", ticket.EventName);
            throw;
        }
    }

    private string GenerateTicketHtml(UserTicketDto ticket, string qrCodeBase64)
    {
        var eventDate = ticket.EventStartDate.ToString("MMMM dd, yyyy");
        var eventTime = ticket.EventStartDate.ToString("h:mm tt");
        var statusText = ticket.CheckedInAt.HasValue ? "CHECKED IN" : "VALID";
        var statusColor = ticket.CheckedInAt.HasValue ? "#28a745" : "#007bff";

        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Event Ticket - {ticket.EventName}</title>
    <style>
        body {{
            font-family: 'Arial', sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f8f9fa;
        }}
        .ticket {{
            max-width: 600px;
            margin: 0 auto;
            background: white;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 8px 32px rgba(0,0,0,0.1);
            position: relative;
        }}
        .ticket-header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .ticket-title {{
            font-size: 28px;
            font-weight: bold;
            margin: 0 0 10px 0;
        }}
        .ticket-subtitle {{
            font-size: 16px;
            opacity: 0.9;
            margin: 0;
        }}
        .ticket-body {{
            padding: 30px;
        }}
        .ticket-info {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 30px;
        }}
        .info-section {{
            flex: 1;
            margin-right: 20px;
        }}
        .info-section:last-child {{
            margin-right: 0;
        }}
        .info-label {{
            font-size: 12px;
            font-weight: bold;
            color: #6c757d;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 5px;
        }}
        .info-value {{
            font-size: 16px;
            font-weight: 600;
            color: #2c3e50;
            margin-bottom: 15px;
        }}
        .qr-section {{
            text-align: center;
            border-top: 2px dashed #e9ecef;
            padding-top: 30px;
            margin-top: 30px;
        }}
        .qr-code {{
            display: inline-block;
            padding: 15px;
            background: white;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            margin-bottom: 15px;
        }}
        .qr-code img {{
            width: 150px;
            height: 150px;
        }}
        .qr-instructions {{
            font-size: 14px;
            color: #6c757d;
            margin-bottom: 10px;
        }}
        .ticket-number {{
            font-size: 14px;
            font-weight: bold;
            color: #495057;
            font-family: 'Courier New', monospace;
        }}
        .status-badge {{
            position: absolute;
            top: 20px;
            right: 20px;
            background: {statusColor};
            color: white;
            padding: 8px 16px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: bold;
            letter-spacing: 1px;
        }}
        .ticket-footer {{
            background: #f8f9fa;
            padding: 20px 30px;
            border-top: 1px solid #e9ecef;
            text-align: center;
            font-size: 12px;
            color: #6c757d;
        }}
        .print-info {{
            margin-top: 20px;
            padding-top: 15px;
            border-top: 1px solid #e9ecef;
            font-size: 11px;
            color: #adb5bd;
        }}
    </style>
</head>
<body>
    <div class=""ticket"">
        <div class=""status-badge"">{statusText}</div>
        
        <div class=""ticket-header"">
            <h1 class=""ticket-title"">{ticket.EventName}</h1>
            <p class=""ticket-subtitle"">Event Admission Ticket</p>
        </div>
        
        <div class=""ticket-body"">
            <div class=""ticket-info"">
                <div class=""info-section"">
                    <div class=""info-label"">Event Date</div>
                    <div class=""info-value"">{eventDate}</div>
                    
                    <div class=""info-label"">Event Time</div>
                    <div class=""info-value"">{eventTime}</div>
                    
                    <div class=""info-label"">Venue</div>
                    <div class=""info-value"">{ticket.VenueName}</div>
                </div>
                
                <div class=""info-section"">
                    <div class=""info-label"">Attendee</div>
                    <div class=""info-value"">{ticket.AttendeeName}</div>
                    
                    <div class=""info-label"">Ticket Type</div>
                    <div class=""info-value"">{ticket.TicketTypeName}</div>
                    
                    <div class=""info-label"">Price</div>
                    <div class=""info-value"">{ticket.Price:C}</div>
                </div>
            </div>
            
            {(ticket.CheckedInAt.HasValue ? 
                $@"<div class=""info-section"">
                    <div class=""info-label"">Checked In</div>
                    <div class=""info-value"">{ticket.CheckedInAt.Value:MMM dd, yyyy • h:mm tt}</div>
                </div>" : "")}
            
            <div class=""qr-section"">
                <div class=""qr-code"">
                    <img src=""data:image/png;base64,{qrCodeBase64}"" alt=""QR Code"" />
                </div>
                
                <div class=""qr-instructions"">
                    Present this QR code at the event entrance for check-in
                </div>
                
                <div class=""ticket-number"">
                    Ticket #{ticket.UniqueReferenceCode}
                </div>
            </div>
        </div>
        
        <div class=""ticket-footer"">
            <div>
                <strong>Event Management System</strong><br>
                Thank you for attending our event!
            </div>
            
            <div class=""print-info"">
                Generated on {DateTime.Now:MMMM dd, yyyy • h:mm tt}<br>
                Keep this ticket safe and present it at the venue entrance
            </div>
        </div>
    </div>
</body>
</html>";
    }
}