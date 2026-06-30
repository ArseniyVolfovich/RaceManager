namespace RaceManager.Domain.Entities;

public sealed class SupportTicket
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "Ожидание";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public List<SupportTicketAnswer> Answers { get; set; } = [];
}

public sealed class SupportTicketAnswer
{
    public string Id { get; set; } = string.Empty;
    public string AdminUserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string EmailHtml { get; set; } = string.Empty;
    public string EmailDeliveryStatus { get; set; } = "Не отправлено";
    public string EmailDeliveryError { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
