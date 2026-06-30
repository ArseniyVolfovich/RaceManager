using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record CreateSupportTicketRequest(
    string? UserId,
    string Name,
    string Email,
    string Subject,
    string Category,
    string Message);

public sealed record AnswerSupportTicketRequest(string AdminUserId, string Message);

public sealed record RejectSupportTicketRequest(string AdminUserId, string? Reason);

public sealed record SupportTicketResponse(string Message, SupportTicket Ticket);
