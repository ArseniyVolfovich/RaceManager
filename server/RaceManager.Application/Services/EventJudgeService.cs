using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Security;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class EventJudgeService(IEventJudgeRepository eventJudges, IEventRepository events, IUserRepository users)
{
    public Task<IReadOnlyList<EventJudgeInfo>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default) =>
        eventJudges.GetByEventAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<EventJudgeInfo>> GetByJudgeAsync(string judgeUserId, CancellationToken cancellationToken = default) =>
        eventJudges.GetByJudgeAsync(judgeUserId, cancellationToken);

    public Task<bool> IsAssignedAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default) =>
        eventJudges.IsAssignedAsync(eventId, judgeUserId, cancellationToken);

    public async Task<EventJudgeInvitationResponse> InviteAsync(
        string eventId,
        string organizerUserId,
        InviteEventJudgeRequest request,
        CancellationToken cancellationToken = default)
    {
        var raceEvent = await RequireOrganizerAccessAsync(eventId, organizerUserId, cancellationToken);
        var identifier = request.Identifier?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new InvalidOperationException("Укажите почту или номер телефона судьи.");
        }

        var isEmail = identifier.Contains('@');
        var normalizedPhone = NormalizePhone(identifier);
        if (!isEmail && normalizedPhone.Length < 10)
        {
            throw new InvalidOperationException("Укажите корректную почту или номер телефона судьи.");
        }

        var candidates = (await users.GetAllAsync(cancellationToken))
            .Where(user => user.Role == "Судья")
            .Where(user => isEmail
                ? user.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)
                : NormalizePhone(user.Profile.Phone) == normalizedPhone)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Судья с такой почтой или номером телефона не найден.");
        }
        if (candidates.Count > 1)
        {
            throw new InvalidOperationException("Найдено несколько судей. Укажите уникальную почту.");
        }

        var judge = candidates[0];
        if (await eventJudges.IsAssignedAsync(raceEvent.Id, judge.Id, cancellationToken))
        {
            throw new InvalidOperationException("Этот судья уже назначен на событие.");
        }

        var pendingInvitation = judge.Profile.TeamInvitations.Any(invitation =>
            invitation.TeamType == "Judge" &&
            invitation.TargetId == raceEvent.Id &&
            invitation.Status == "Pending");
        if (pendingInvitation)
        {
            throw new InvalidOperationException("Приглашение этому судье уже отправлено.");
        }

        var organizer = await users.GetByIdAsync(organizerUserId, cancellationToken)
            ?? throw new InvalidOperationException("Организатор не найден.");
        judge.Profile.TeamInvitations.Add(new TeamInvitation
        {
            Id = $"judge-invite-{Guid.NewGuid():N}"[..25],
            OwnerUserId = organizer.Id,
            OwnerName = DisplayName(organizer),
            TeamType = "Judge",
            TargetId = raceEvent.Id,
            TeamName = raceEvent.Title,
            TeamColor = organizer.Profile.OrganizationColor,
            TeamLogo = organizer.Profile.OrganizationLogo,
            TeamBanner = organizer.Profile.OrganizationBanner,
            Status = "Pending",
            IsRead = false
        });
        await users.UpdateAsync(judge, cancellationToken);

        return new EventJudgeInvitationResponse("Приглашение судье отправлено.", judge.Id);
    }

    public async Task<EventJudgeResponse> RemoveAsync(string eventId, string judgeUserId, string organizerUserId, CancellationToken cancellationToken = default)
    {
        await RequireOrganizerAccessAsync(eventId, organizerUserId, cancellationToken);
        await eventJudges.RemoveAsync(eventId, judgeUserId, cancellationToken);
        return new EventJudgeResponse("Судья удален из события.", await eventJudges.GetByEventAsync(eventId, cancellationToken));
    }

    private async Task<RaceEvent> RequireOrganizerAccessAsync(string eventId, string organizerUserId, CancellationToken cancellationToken)
    {
        var raceEvent = await events.GetByIdAsync(eventId, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
        if (!DemoAccess.IsGlobalOrganizer(organizerUserId) &&
            !string.Equals(raceEvent.OrganizerUserId, organizerUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Управлять судьями может только организатор события.");
        }
        return raceEvent;
    }

    private static string NormalizePhone(string value) => new(value.Where(char.IsDigit).ToArray());

    private static string DisplayName(User user)
    {
        var fullName = string.Join(" ", new[] { user.Profile.LastName, user.Profile.FirstName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(fullName) ? user.Login : fullName;
    }
}
