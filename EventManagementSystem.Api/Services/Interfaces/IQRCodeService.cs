namespace EventManagementSystem.Api.Services.Interfaces
{
    public interface IQRCodeService
    {
        Task<byte[]> GenerateTicketQRCodeAsync(string ticketData);
        Task<byte[]> GenerateEventQRCodeAsync(int eventId);
        Task<string> GenerateSecureTicketDataAsync(int registrationId, int ticketTypeId, string userEmail);
        Task<bool> ValidateTicketDataAsync(string qrData);
        Task<string> GenerateTicketQRCodeBase64Async(string ticketData);
        Task<string> GenerateTicketQRCodeBase64Async(int registrationId, int ticketTypeId, string userEmail);
        Task<TicketQRPayload?> ExtractTicketPayloadAsync(string qrData);

    }
}