using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class UserProfileService(IUserRepository users, IEventJudgeRepository eventJudges)
{
    private static readonly string[] VehicleSpecKeys =
    [
        "Тип",
        "Мощность л.с.",
        "Вес, кг",
        "Удельная мощность, л.с./т.",
        "Привод",
        "Тип двигателя",
        "Модель двигателя",
        "Объем, см3",
        "Крутящий момент, Нм"
    ];

    public async Task<UserUpdateResponse> AddVehicleAsync(string userId, AddVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Укажите название автомобиля.");
        }

        var specs = new Dictionary<string, string>();
        var requestSpecs = request.Specs ?? [];
        foreach (var key in VehicleSpecKeys)
        {
            requestSpecs.TryGetValue(key, out var value);
            specs[key] = string.IsNullOrWhiteSpace(value) ? "Не указано" : value.Trim();
        }

        var isOrganizationVehicle = request.IsTeamVehicle && request.TeamType?.Equals("Organizer", StringComparison.OrdinalIgnoreCase) == true;
        var membership = request.IsTeamVehicle
            ? user.Profile.TeamMemberships.FirstOrDefault(item => item.TeamType.Equals(isOrganizationVehicle ? "Organizer" : "Racing", StringComparison.OrdinalIgnoreCase))
            : null;
        var teamName = request.IsTeamVehicle
            ? (isOrganizationVehicle ? user.Profile.OrganizationName : user.Profile.RacingTeamName)
            : string.Empty;
        var teamLogo = request.IsTeamVehicle
            ? (isOrganizationVehicle ? user.Profile.OrganizationLogo : user.Profile.RacingTeamLogo)
            : string.Empty;
        if (request.IsTeamVehicle && string.IsNullOrWhiteSpace(teamName)) teamName = membership?.TeamName ?? string.Empty;
        if (request.IsTeamVehicle && string.IsNullOrWhiteSpace(teamLogo)) teamLogo = membership?.TeamLogo ?? string.Empty;
        if (request.IsTeamVehicle && string.IsNullOrWhiteSpace(teamName)) throw new InvalidOperationException("Сначала создайте выбранную команду.");

        user.Vehicles.Add(new Vehicle
        {
            Id = $"vehicle-{Guid.NewGuid():N}"[..16],
            Name = request.Name.Trim(),
            Image = request.Image?.Trim() ?? string.Empty,
            Specs = specs,
            IsTeamVehicle = request.IsTeamVehicle,
            TeamName = teamName,
            TeamLogo = teamLogo
        });

        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Автомобиль добавлен.", user.ToDto());
    }

    public async Task<UserUpdateResponse> DeleteVehicleAsync(string userId, string vehicleId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var removed = user.Vehicles.RemoveAll(vehicle =>
            string.Equals(vehicle.Id, vehicleId, StringComparison.OrdinalIgnoreCase));
        if (removed == 0) throw new InvalidOperationException("Автомобиль не найден.");

        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Автомобиль удален.", user.ToDto());
    }

    public async Task<UserUpdateResponse> AddApplicationAsync(string userId, AddUserApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.EventName))
        {
            throw new InvalidOperationException("Укажите событие для заявки.");
        }

        user.Applications.Add(new UserApplication
        {
            Id = $"application-{Guid.NewGuid():N}"[..20],
            EventName = request.EventName.Trim(),
            Date = request.Date.Trim(),
            Location = request.Location.Trim(),
            Discipline = request.Discipline.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? "На рассмотрении" : request.Status.Trim()
        });

        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Заявка добавлена.", user.ToDto());
    }

    public async Task<UserUpdateResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);

        user.Profile.LastName = request.LastName?.Trim() ?? user.Profile.LastName;
        user.Profile.FirstName = request.FirstName?.Trim() ?? user.Profile.FirstName;
        user.Profile.MiddleName = request.MiddleName?.Trim() ?? user.Profile.MiddleName;
        if (request.Phone is not null) PhoneNumberValidator.EnsureValid(request.Phone);
        user.Profile.Phone = request.Phone?.Trim() ?? user.Profile.Phone;
        user.Avatar = request.Avatar?.Trim() ?? user.Avatar;
        user.Profile.OrganizationName = request.OrganizationName?.Trim() ?? user.Profile.OrganizationName;
        user.Profile.OrganizationColor = string.IsNullOrWhiteSpace(request.OrganizationColor) ? user.Profile.OrganizationColor : request.OrganizationColor.Trim();
        user.Profile.OrganizationLogo = request.OrganizationLogo?.Trim() ?? user.Profile.OrganizationLogo;
        user.Profile.OrganizationBanner = request.OrganizationBanner?.Trim() ?? user.Profile.OrganizationBanner;
        if (request.OrganizationMembers is not null)
        {
            user.Profile.OrganizationMembers = request.OrganizationMembers
                .Where(member => !string.IsNullOrWhiteSpace(member.FullName))
                .Select(member => new OrganizationMember
                {
                    UserId = member.UserId?.Trim() ?? string.Empty,
                    Status = member.Status?.Trim() ?? string.Empty,
                    FullName = member.FullName.Trim(),
                    Phone = member.Phone.Trim(),
                    Email = member.Email.Trim().ToLowerInvariant()
                }).ToList();
        }
        user.Profile.RacingTeamName = request.RacingTeamName?.Trim() ?? user.Profile.RacingTeamName;
        user.Profile.RacingTeamColor = string.IsNullOrWhiteSpace(request.RacingTeamColor) ? user.Profile.RacingTeamColor : request.RacingTeamColor.Trim();
        user.Profile.RacingTeamLogo = request.RacingTeamLogo?.Trim() ?? user.Profile.RacingTeamLogo;
        user.Profile.RacingTeamBanner = request.RacingTeamBanner?.Trim() ?? user.Profile.RacingTeamBanner;
        if (request.RacingTeamMembers is not null)
        {
            user.Profile.RacingTeamMembers = request.RacingTeamMembers
                .Where(member => !string.IsNullOrWhiteSpace(member.FullName))
                .Select(member => new OrganizationMember
                {
                    UserId = member.UserId?.Trim() ?? string.Empty,
                    Status = member.Status?.Trim() ?? string.Empty,
                    FullName = member.FullName.Trim(),
                    Phone = member.Phone.Trim(),
                    Email = member.Email.Trim().ToLowerInvariant()
                }).ToList();
        }
        user.Profile.DriverNumber = request.DriverNumber?.Trim() ?? user.Profile.DriverNumber;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            user.Email = request.Email.Trim();
        }

        TeamComposition.Normalize(user);
        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Профиль обновлен.", user.ToDto());
    }


    public async Task<TeamInvitationUpdateResponse> SendTeamInvitationsAsync(string ownerId, SendTeamInvitationsRequest request, CancellationToken cancellationToken = default)
    {
        var owner = await RequireUserAsync(ownerId, cancellationToken);
        var isOrganization = request.TeamType.Equals("Organizer", StringComparison.OrdinalIgnoreCase);
        var isRacing = request.TeamType.Equals("Racing", StringComparison.OrdinalIgnoreCase);
        if (!isOrganization && !isRacing) throw new InvalidOperationException("Неизвестный тип команды.");
        if (isOrganization && owner.Role != "Организатор") throw new InvalidOperationException("Команду организаторов может создавать только организатор.");

        var teamName = isOrganization ? owner.Profile.OrganizationName : owner.Profile.RacingTeamName;
        var teamColor = isOrganization ? owner.Profile.OrganizationColor : owner.Profile.RacingTeamColor;
        var teamLogo = isOrganization ? owner.Profile.OrganizationLogo : owner.Profile.RacingTeamLogo;
        var teamBanner = isOrganization ? owner.Profile.OrganizationBanner : owner.Profile.RacingTeamBanner;
        if (string.IsNullOrWhiteSpace(teamName)) throw new InvalidOperationException("Сначала укажите название команды.");

        var allUsers = await users.GetAllAsync(cancellationToken);
        var missing = new List<string>();
        TeamComposition.Normalize(owner);

        foreach (var invitee in request.Invitees)
        {
            var requestedName = invitee.FullName?.Trim() ?? string.Empty;
            var requestedPhone = invitee.Phone?.Trim() ?? string.Empty;
            var requestedEmail = invitee.Email?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(requestedName) && string.IsNullOrWhiteSpace(requestedPhone) && string.IsNullOrWhiteSpace(requestedEmail))
            {
                throw new InvalidOperationException("Укажите фамилию и имя, телефон или почту пользователя.");
            }
            if (string.IsNullOrWhiteSpace(requestedEmail) && !string.IsNullOrWhiteSpace(requestedPhone))
            {
                PhoneNumberValidator.EnsureValid(requestedPhone);
            }
            if (string.IsNullOrWhiteSpace(requestedEmail) && string.IsNullOrWhiteSpace(requestedPhone) &&
                requestedName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 2)
            {
                throw new InvalidOperationException("Для поиска по ФИО укажите фамилию и имя.");
            }

            var candidates = allUsers.Where(candidate => candidate.Id != owner.Id);
            var matches = !string.IsNullOrWhiteSpace(requestedEmail)
                ? candidates.Where(candidate => candidate.Email.Equals(requestedEmail, StringComparison.OrdinalIgnoreCase)).ToList()
                : !string.IsNullOrWhiteSpace(requestedPhone)
                    ? candidates.Where(candidate => NormalizePhone(candidate.Profile.Phone) == NormalizePhone(requestedPhone)).ToList()
                    : candidates.Where(candidate => NamesMatch(FullName(candidate), requestedName)).ToList();

            var descriptor = !string.IsNullOrWhiteSpace(requestedEmail) ? requestedEmail :
                (!string.IsNullOrWhiteSpace(requestedPhone) ? requestedPhone : requestedName);
            if (matches.Count != 1)
            {
                missing.Add(matches.Count == 0 ? descriptor : $"{descriptor} (найдено несколько пользователей)");
                continue;
            }

            var matched = matches[0];
            var exists = matched.Profile.TeamInvitations.Any(item =>
                item.Status == "Pending" && item.OwnerUserId == owner.Id &&
                item.TeamType.Equals(request.TeamType, StringComparison.OrdinalIgnoreCase));

            if (!exists)
            {
                matched.Profile.TeamInvitations.Add(new TeamInvitation
                {
                    Id = $"team-invite-{Guid.NewGuid():N}"[..24],
                    OwnerUserId = owner.Id,
                    OwnerName = FullName(owner),
                    TeamType = isOrganization ? "Organizer" : "Racing",
                    TeamName = teamName,
                    TeamColor = teamColor,
                    TeamLogo = teamLogo,
                    TeamBanner = teamBanner,
                    Status = "Pending",
                    IsRead = false
                });
                await users.UpdateAsync(matched, cancellationToken);
            }

        }

        await users.UpdateAsync(owner, cancellationToken);
        var message = missing.Count == 0 ? "Приглашения отправлены." : "Некоторые пользователи не найдены.";
        return new TeamInvitationUpdateResponse(message, missing, owner.ToDto());
    }

    public async Task<TeamInvitationUpdateResponse> RespondTeamInvitationAsync(string userId, string invitationId, RespondTeamInvitationRequest request, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var invitation = user.Profile.TeamInvitations.FirstOrDefault(item => item.Id == invitationId && item.Status == "Pending")
            ?? throw new InvalidOperationException("Приглашение не найдено или уже обработано.");
        var owner = await RequireUserAsync(invitation.OwnerUserId, cancellationToken);
        TeamComposition.Normalize(owner);
        invitation.Status = request.Accept ? "Accepted" : "Declined";
        invitation.IsRead = true;

var userName = DisplayName(user);
if (invitation.TeamType == "Judge")
{
    if (user.Role != "Судья") throw new InvalidOperationException("Принять приглашение может только судья.");
    if (string.IsNullOrWhiteSpace(invitation.TargetId)) throw new InvalidOperationException("В приглашении не указано событие.");

    if (request.Accept)
    {
        await eventJudges.AssignAsync(invitation.TargetId, user.Id, cancellationToken);
    }

    owner.Profile.TeamNotifications.Add(new TeamNotification
    {
        Id = $"judge-note-{Guid.NewGuid():N}"[..23],
        Message = request.Accept
            ? $"{userName} принял приглашение судить событие «{invitation.TeamName}»."
            : $"{userName} отклонил приглашение судить событие «{invitation.TeamName}».",
        IsRead = false
    });

    await users.UpdateAsync(owner, cancellationToken);
    await users.UpdateAsync(user, cancellationToken);
    return new TeamInvitationUpdateResponse(request.Accept ? "Приглашение принято." : "Приглашение отклонено.", [], user.ToDto());
}

var members = invitation.TeamType == "Organizer" ? owner.Profile.OrganizationMembers : owner.Profile.RacingTeamMembers;
        var existingMember = members.FirstOrDefault(member => member.UserId == user.Id);

        if (request.Accept)
        {
            user.Profile.TeamMemberships.RemoveAll(item => item.TeamType == invitation.TeamType);
            user.Profile.TeamMemberships.Add(new TeamMembership
            {
                OwnerUserId = owner.Id,
                TeamType = invitation.TeamType,
                TeamName = invitation.TeamName,
                TeamColor = invitation.TeamColor,
                TeamLogo = invitation.TeamLogo,
                TeamBanner = invitation.TeamBanner
            });

            if (existingMember is null)
            {
                members.Add(new OrganizationMember
                {
                    UserId = user.Id,
                    Status = "Accepted",
                    FullName = userName,
                    Phone = user.Profile.Phone,
                    Email = user.Email
                });
            }
            else
            {
                existingMember.Status = "Accepted";
                existingMember.FullName = userName;
                existingMember.Phone = user.Profile.Phone;
                existingMember.Email = user.Email;
            }

            owner.Profile.TeamNotifications.Add(new TeamNotification
            {
                Id = $"team-note-{Guid.NewGuid():N}"[..22],
                Message = $"{userName} принял приглашение в команду «{invitation.TeamName}».",
                IsRead = false
            });
        }
        else
        {
            if (existingMember is not null) members.Remove(existingMember);
            owner.Profile.TeamNotifications.Add(new TeamNotification
            {
                Id = $"team-note-{Guid.NewGuid():N}"[..22],
                Message = $"{userName} отказался от вступления в команду «{invitation.TeamName}».",
                IsRead = false
            });
        }

        TeamComposition.Normalize(owner);
        await users.UpdateAsync(owner, cancellationToken);
        await users.UpdateAsync(user, cancellationToken);
        return new TeamInvitationUpdateResponse(request.Accept ? "Приглашение принято." : "Приглашение отклонено.", [], user.ToDto());
    }


    public async Task<UserUpdateResponse> CancelTeamInvitationAsync(string ownerId, string targetUserId, string teamType, CancellationToken cancellationToken = default)
    {
        var owner = await RequireUserAsync(ownerId, cancellationToken);
        var target = await RequireUserAsync(targetUserId, cancellationToken);
        target.Profile.TeamInvitations.RemoveAll(item =>
            item.OwnerUserId == owner.Id && item.TeamType.Equals(teamType, StringComparison.OrdinalIgnoreCase) && item.Status == "Pending");
        var members = teamType.Equals("Organizer", StringComparison.OrdinalIgnoreCase)
            ? owner.Profile.OrganizationMembers
            : owner.Profile.RacingTeamMembers;
        members.RemoveAll(member => member.UserId == target.Id && member.Status == "Pending");
        await users.UpdateAsync(target, cancellationToken);
        await users.UpdateAsync(owner, cancellationToken);
        return new UserUpdateResponse("Приглашение отменено.", owner.ToDto());
    }

    public async Task<UserUpdateResponse> MarkNotificationsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        foreach (var invitation in user.Profile.TeamInvitations.Where(item => item.Status == "Pending")) invitation.IsRead = true;
        foreach (var notification in user.Profile.TeamNotifications) notification.IsRead = true;
        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Уведомления отмечены как прочитанные.", user.ToDto());
    }

    public async Task<UserUpdateResponse> MarkNotificationReadAsync(
        string userId,
        string notificationId,
        CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(userId, cancellationToken);
        var invitation = user.Profile.TeamInvitations.FirstOrDefault(item => item.Id == notificationId);
        var notification = user.Profile.TeamNotifications.FirstOrDefault(item => item.Id == notificationId);
        if (invitation is null && notification is null)
        {
            throw new InvalidOperationException("Уведомление не найдено.");
        }

        if (invitation is not null) invitation.IsRead = true;
        if (notification is not null) notification.IsRead = true;
        await users.UpdateAsync(user, cancellationToken);
        return new UserUpdateResponse("Уведомление отмечено как прочитанное.", user.ToDto());
    }

private static string FullName(User user) =>
    string.Join(" ", new[] { user.Profile.LastName, user.Profile.FirstName, user.Profile.MiddleName }.Where(value => !string.IsNullOrWhiteSpace(value)));

private static string DisplayName(User user)
{
    var fullName = FullName(user);
    return string.IsNullOrWhiteSpace(fullName) ? user.Login : fullName;
}

    private static bool NamesMatch(string candidateName, string requestedName)
    {
        var candidateParts = candidateName.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var requestedParts = requestedName.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return requestedParts.Length >= 2 && requestedParts.All(candidateParts.Contains);
    }

    private static string NormalizePhone(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    private async Task<User> RequireUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user ?? throw new InvalidOperationException("Пользователь не найден.");
    }
}
