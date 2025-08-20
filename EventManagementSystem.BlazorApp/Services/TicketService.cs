using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.BlazorApp.Services;

public class TicketService
{
    private readonly ApiService _apiService;

    public TicketService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task<List<UserTicketDto>> GetMyTicketsAsync()
    {
        try
        {
            var response = await _apiService.GetAsync<List<UserTicketDto>>("api/tickets/my-tickets");
            return response?.Data ?? new List<UserTicketDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user tickets: {ex.Message}");
            return new List<UserTicketDto>();
        }
    }

    public string GetQRImageUrl(int ticketId)
    {
        return $"api/tickets/{ticketId}/qr-image";
    }

    public async Task<bool> SendTicketEmailAsync(int ticketId)
    {
        try
        {
            var response = await _apiService.PostAsync<bool>($"api/tickets/{ticketId}/send-email", new { });
            return response?.Success == true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending ticket email: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetQRCodeBase64Async(int ticketId)
    {
        try
        {
            var response = await _apiService.GetAsync<string>($"api/tickets/{ticketId}/qr-base64");
            return response?.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting QR code: {ex.Message}");
            return null;
        }
    }

    public string GetPdfDownloadUrl(int ticketId)
    {
        return $"api/tickets/{ticketId}/download-pdf";
    }

    public string GetFullPdfDownloadUrl(int ticketId)
    {
        // Use the configured base URL from HttpClient
        return $"https://localhost:7203/api/tickets/{ticketId}/download-pdf";
    }

    public async Task DownloadTicketPdfAsync(int ticketId, string fileName)
    {
        try
        {
            var response = await _apiService.GetStreamAsync($"api/tickets/{ticketId}/download-pdf");
            
            if (response != null)
            {
                // Convert stream to byte array and trigger download via JavaScript
                using var memoryStream = new MemoryStream();
                await response.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);
                
                // Use JavaScript to trigger the download
                await _apiService.TriggerFileDownloadAsync(base64, fileName, "application/pdf");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error downloading ticket PDF: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check in a ticket using QR code data
    /// </summary>
    public async Task<ApiResponse<CheckInTicketResponse>?> CheckInTicketAsync(CheckInTicketRequest request)
    {
        try
        {
            var response = await _apiService.PostAsync<CheckInTicketResponse>("api/tickets/checkin", request);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking in ticket: {ex.Message}");
            return new ApiResponse<CheckInTicketResponse>
            {
                Success = false,
                Message = "An error occurred while checking in the ticket."
            };
        }
    }

    /// <summary>
    /// Validate a ticket without checking it in
    /// </summary>
    public async Task<ApiResponse<ValidateTicketResponse>?> ValidateTicketAsync(ValidateTicketRequest request)
    {
        try
        {
            var response = await _apiService.PostAsync<ValidateTicketResponse>("api/tickets/validate", request);
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating ticket: {ex.Message}");
            return new ApiResponse<ValidateTicketResponse>
            {
                Success = false,
                Message = "An error occurred while validating the ticket."
            };
        }
    }

    /// <summary>
    /// Get all check-ins for an event
    /// </summary>
    public async Task<ApiResponse<List<TicketCheckInDetails>>?> GetEventCheckInsAsync(int eventId)
    {
        try
        {
            var response = await _apiService.GetAsync<List<TicketCheckInDetails>>($"api/tickets/event/{eventId}/checkins");
            
            if (response == null)
                throw new Exception("No response received from server");
            if (!response.Success)
                throw new Exception(response.Message ?? "Failed to retrieve event check-ins");
            
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting event check-ins: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Undo a ticket check-in
    /// </summary>
    public async Task<ApiResponse<bool>?> UndoCheckInAsync(int ticketId)
    {
        try
        {
            var response = await _apiService.PostAsync<bool>($"api/tickets/{ticketId}/undo-checkin", null);
            
            if (response == null)
                throw new Exception("No response received from server");
            if (!response.Success)
                throw new Exception(response.Message ?? "Failed to undo check-in");
            
            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error undoing check-in: {ex.Message}");
            throw;
        }
    }
}