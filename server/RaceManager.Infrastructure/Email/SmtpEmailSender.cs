using System.Net;
using System.Net.Mail;
using RaceManager.Application.Interfaces;

namespace RaceManager.Infrastructure.Email;

public sealed class SmtpEmailSender(SmtpEmailOptions options) : IEmailSender
{
    public async Task SendHtmlAsync(string to, string subject, string html, CancellationToken cancellationToken = default)
    {
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException("SMTP не настроен. Укажите Email:Smtp в appsettings.Development.json или переменных окружения.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(options.FromEmail, options.FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(to));

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            Credentials = new NetworkCredential(options.UserName, options.Password),
            Timeout = 10_000
        };

        using var registration = cancellationToken.Register(client.SendAsyncCancel);
        await client.SendMailAsync(message, cancellationToken);
    }
}
