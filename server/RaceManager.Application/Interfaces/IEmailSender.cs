namespace RaceManager.Application.Interfaces;

public interface IEmailSender
{
    Task SendHtmlAsync(string to, string subject, string html, CancellationToken cancellationToken = default);
}
