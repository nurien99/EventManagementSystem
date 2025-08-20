using EventManagementSystem.Core.DTOs;

namespace EventManagementSystem.Api.Services.Interfaces;

public interface IPdfService
{
    Task<byte[]> GenerateTicketPdfAsync(UserTicketDto ticket);
}