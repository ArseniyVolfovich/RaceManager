using System.Net;
using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class SupportService(ISupportTicketRepository tickets, IUserRepository users, IEmailSender emailSender)
{
    private const string WaitingStatus = "Ожидание";
    private const string ReviewedStatus = "Рассмотренное";
    private const string RejectedStatus = "Отклонено";
    private const string SupportInboxEmail = "patriotgym888@gmail.com";

    public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return tickets.GetAllAsync(cancellationToken);
    }

    public async Task<SupportTicketResponse> CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Укажите имя отправителя.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidOperationException("Укажите почту отправителя.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Опишите проблему.");
        }

        var ticket = new SupportTicket
        {
            Id = $"ticket-{Guid.NewGuid():N}"[..18],
            UserId = request.UserId?.Trim() ?? string.Empty,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? "Обращение в поддержку" : request.Subject.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Общее обращение" : request.Category.Trim(),
            Message = request.Message.Trim(),
            Status = WaitingStatus
        };

        await tickets.AddAsync(ticket, cancellationToken);
        await TrySendNewTicketNotificationAsync(ticket, cancellationToken);
        return new SupportTicketResponse("Вы успешно отправили обращение", ticket);
    }

    public async Task<SupportTicketResponse> AnswerAsync(string ticketId, AnswerSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSupportAdminAsync(request.AdminUserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Введите ответ пользователю.");
        }

        var ticket = await GetRequiredTicketAsync(ticketId, cancellationToken);
        var answer = new SupportTicketAnswer
        {
            Id = $"answer-{Guid.NewGuid():N}"[..18],
            AdminUserId = request.AdminUserId.Trim(),
            Message = request.Message.Trim(),
            EmailHtml = BuildAnswerEmailHtml(ticket, request.Message.Trim())
        };

        await TrySendTicketEmailAsync(ticket, answer, cancellationToken);
        ticket.Answers.Add(answer);
        ticket.Status = ReviewedStatus;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await tickets.UpdateAsync(ticket, cancellationToken);

        return new SupportTicketResponse("Ответ успешно сохранён.", ticket);
    }

    public async Task<SupportTicketResponse> RejectAsync(string ticketId, RejectSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureSupportAdminAsync(request.AdminUserId, cancellationToken);
        var ticket = await GetRequiredTicketAsync(ticketId, cancellationToken);

        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Обращение отклонено техническим администратором."
            : request.Reason.Trim();

        var answer = new SupportTicketAnswer
        {
            Id = $"answer-{Guid.NewGuid():N}"[..18],
            AdminUserId = request.AdminUserId.Trim(),
            Message = reason,
            EmailHtml = BuildAnswerEmailHtml(ticket, reason)
        };

        await TrySendTicketEmailAsync(ticket, answer, cancellationToken);
        ticket.Answers.Add(answer);
        ticket.Status = RejectedStatus;
        ticket.UpdatedAtUtc = DateTime.UtcNow;
        await tickets.UpdateAsync(ticket, cancellationToken);

        return new SupportTicketResponse("Обращение отклонено.", ticket);
    }

    private async Task TrySendNewTicketNotificationAsync(SupportTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendHtmlAsync(
                SupportInboxEmail,
                $"RaceManager: новое обращение - {ticket.Subject}",
                BuildNewTicketEmailHtml(ticket),
                cancellationToken);
        }
        catch
        {
            // Обращение не должно теряться, если SMTP временно недоступен или не настроен.
        }
    }

    private async Task TrySendTicketEmailAsync(SupportTicket ticket, SupportTicketAnswer answer, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendHtmlAsync(ticket.Email, $"RaceManager: ответ по обращению {ticket.Subject}", answer.EmailHtml, cancellationToken);
            answer.EmailDeliveryStatus = "Отправлено";
            answer.EmailDeliveryError = string.Empty;
        }
        catch (Exception error)
        {
            answer.EmailDeliveryStatus = "Не отправлено";
            answer.EmailDeliveryError = error.Message;
        }
    }

    private async Task<SupportTicket> GetRequiredTicketAsync(string ticketId, CancellationToken cancellationToken)
    {
        return await tickets.GetByIdAsync(ticketId, cancellationToken)
            ?? throw new InvalidOperationException("Обращение не найдено.");
    }

    private async Task EnsureSupportAdminAsync(string adminUserId, CancellationToken cancellationToken)
    {
        var admin = await users.GetByIdAsync(adminUserId, cancellationToken);
        if (admin is null || (admin.Role != "Технический администратор" && admin.Role != "Технический админ"))
        {
            throw new InvalidOperationException("Работать с обращениями может только технический администратор.");
        }
    }

    private static string BuildNewTicketEmailHtml(SupportTicket ticket)
    {
        var safeName = WebUtility.HtmlEncode(ticket.Name);
        var safeEmail = WebUtility.HtmlEncode(ticket.Email);
        var safeSubject = WebUtility.HtmlEncode(ticket.Subject);
        var safeCategory = WebUtility.HtmlEncode(ticket.Category);
        var safeMessage = WebUtility.HtmlEncode(ticket.Message).Replace("\n", "<br>");

        return $$"""
        <!doctype html>
        <html lang="ru">
        <body style="margin:0;background:#f4f4f6;font-family:Arial,sans-serif;color:#15161b;">
          <div style="max-width:760px;margin:0 auto;padding:36px 18px;">
            <div style="overflow:hidden;border-radius:0 0 18px 18px;background:#111216;border-bottom:4px solid #e10600;">
              <div style="padding:54px 28px;text-align:center;background:radial-gradient(circle at center,#7b0603 0,#17181d 44%,#101116 100%);">
                <strong style="font-size:34px;letter-spacing:.08em;color:#fff;">RACEMANAGER <span style="display:inline-block;margin-left:10px;padding:8px 14px;border-radius:10px;background:#e10600;">ID</span></strong>
              </div>
            </div>
            <div style="padding:48px 36px;background:#fff;">
              <h1 style="margin:0 0 18px;font-size:34px;line-height:1.15;">Новое обращение в поддержку</h1>
              <p style="margin:0 0 18px;font-size:16px;line-height:1.6;color:#656974;">Пользователь <b>{{safeName}}</b> отправил обращение: <b>{{safeSubject}}</b>.</p>
              <div style="margin-bottom:18px;padding:16px;background:#f6f7f9;border-left:4px solid #e10600;line-height:1.7;">
                <b>Email:</b> {{safeEmail}}<br>
                <b>Категория:</b> {{safeCategory}}
              </div>
              <div style="padding:22px;border-left:4px solid #e10600;background:#f6f7f9;font-size:16px;line-height:1.7;">{{safeMessage}}</div>
            </div>
          </div>
        </body>
        </html>
        """;
    }

    private static string BuildAnswerEmailHtml(SupportTicket ticket, string answer)
    {
        var safeName = WebUtility.HtmlEncode(ticket.Name);
        var safeSubject = WebUtility.HtmlEncode(ticket.Subject);
        var safeAnswer = WebUtility.HtmlEncode(answer).Replace("\n", "<br>");

        return $$"""
        <!doctype html>
        <html lang="ru">
        <body style="margin:0;background:#f4f4f6;font-family:Arial,sans-serif;color:#15161b;">
          <div style="max-width:760px;margin:0 auto;padding:36px 18px;">
            <div style="overflow:hidden;border-radius:0 0 18px 18px;background:#111216;border-bottom:4px solid #e10600;">
              <div style="padding:54px 28px;text-align:center;background:radial-gradient(circle at center,#7b0603 0,#17181d 44%,#101116 100%);">
                <strong style="font-size:34px;letter-spacing:.08em;color:#fff;">RACEMANAGER <span style="display:inline-block;margin-left:10px;padding:8px 14px;border-radius:10px;background:#e10600;">ID</span></strong>
              </div>
            </div>
            <div style="padding:48px 36px;background:#fff;">
              <h1 style="margin:0 0 18px;font-size:34px;line-height:1.15;">Ответ технической поддержки</h1>
              <p style="margin:0 0 22px;font-size:16px;line-height:1.6;color:#656974;">Здравствуйте, <b>{{safeName}}</b>. Мы рассмотрели ваше обращение: <b>{{safeSubject}}</b>.</p>
              <div style="padding:22px;border-left:4px solid #e10600;background:#f6f7f9;font-size:16px;line-height:1.7;">{{safeAnswer}}</div>
              <p style="margin:28px 0 0;color:#858994;font-size:13px;">С уважением, команда RaceManager.</p>
            </div>
          </div>
        </body>
        </html>
        """;
    }
}
