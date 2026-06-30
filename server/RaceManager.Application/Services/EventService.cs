using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Security;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class EventService(IEventRepository events, IUserRepository users)
{
    public Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return events.GetAllAsync(cancellationToken);
    }

    public async Task<RaceEvent> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await events.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
    }

    public async Task<EventResponse> CreateAsync(CreateRaceEventRequest request, CancellationToken cancellationToken = default)
    {
        var organizer = await users.GetByIdAsync(request.OrganizerUserId, cancellationToken);
        if (organizer is null || organizer.Role != "Организатор")
        {
            throw new InvalidOperationException("Создавать события может только организатор.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Укажите название события.");
        }

        var raceEvent = new RaceEvent
        {
            Id = $"event-{Guid.NewGuid():N}"[..14],
            ChampionshipId = request.ChampionshipId?.Trim() ?? string.Empty,
            OrganizerUserId = request.OrganizerUserId,
            Type = request.Type.Trim(),
            Title = request.Title.Trim(),
            Discipline = request.Discipline.Trim(),
            ParticipantLimit = Math.Max(0, request.ParticipantLimit),
            Track = request.Track.Trim(),
            Distance = request.Distance?.Trim() ?? string.Empty,
            Laps = request.Laps,
            TrackConfigImage = string.IsNullOrWhiteSpace(request.TrackConfigImage) ? "Конфигурация трека не найдена." : request.TrackConfigImage.Trim(),
            BannerImage = request.BannerImage?.Trim() ?? string.Empty,
            CalendarBannerImage = request.CalendarBannerImage?.Trim() ?? string.Empty,
            OrganizerName = request.OrganizerName?.Trim() ?? organizer.Profile.OrganizationName,
            OrganizerColor = string.IsNullOrWhiteSpace(request.OrganizerColor) ? organizer.Profile.OrganizationColor : request.OrganizerColor.Trim(),
            OrganizerLogo = request.OrganizerLogo?.Trim() ?? organizer.Profile.OrganizationLogo,
            Date = request.Date.Trim(),
            RegistrationStatus = string.IsNullOrWhiteSpace(request.RegistrationStatus) ? "Открыта" : request.RegistrationStatus.Trim(),
            Intro = request.Intro?.Trim() ?? string.Empty,
            Stages = request.Stages?.Select(stage => new RaceEventStage
            {
                Id = $"stage-{Guid.NewGuid():N}"[..14],
                Title = stage.Title.Trim(),
                Date = stage.Date.Trim(),
                Intro = stage.Intro?.Trim() ?? string.Empty,
                RegistrationStatus = string.IsNullOrWhiteSpace(stage.RegistrationStatus) ? "Открыта" : stage.RegistrationStatus.Trim(),
                BannerImage = stage.BannerImage?.Trim() ?? string.Empty
            }).ToList() ?? [],
            PersonalStandings = request.PersonalStandings?.ToList() ?? [],
            TeamStandings = request.TeamStandings?.ToList() ?? []
        };

        await events.AddAsync(raceEvent, cancellationToken);
        return new EventResponse("Событие создано.", raceEvent);
    }

    public async Task<EventResponse> UpdateAsync(string eventId, UpdateRaceEventRequest request, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        await RequireOrganizerAsync(request.OrganizerUserId, raceEvent, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Укажите название события.");
        }

        raceEvent.Type = request.Type.Trim();
        raceEvent.ChampionshipId = request.ChampionshipId?.Trim() ?? raceEvent.ChampionshipId;
        raceEvent.Title = request.Title.Trim();
        raceEvent.Discipline = request.Discipline.Trim();
        raceEvent.ParticipantLimit = Math.Max(0, request.ParticipantLimit);
        raceEvent.Track = request.Track.Trim();
        raceEvent.Distance = request.Distance?.Trim() ?? string.Empty;
        raceEvent.Laps = request.Laps;
        raceEvent.TrackConfigImage = string.IsNullOrWhiteSpace(request.TrackConfigImage) ? "Конфигурация трека не найдена." : request.TrackConfigImage.Trim();
        raceEvent.BannerImage = request.BannerImage?.Trim() ?? string.Empty;
        raceEvent.CalendarBannerImage = request.CalendarBannerImage?.Trim() ?? raceEvent.CalendarBannerImage;
        raceEvent.OrganizerName = request.OrganizerName?.Trim() ?? raceEvent.OrganizerName;
        raceEvent.OrganizerColor = string.IsNullOrWhiteSpace(request.OrganizerColor) ? raceEvent.OrganizerColor : request.OrganizerColor.Trim();
        raceEvent.OrganizerLogo = request.OrganizerLogo?.Trim() ?? raceEvent.OrganizerLogo;
        raceEvent.Date = request.Date.Trim();
        raceEvent.RegistrationStatus = string.IsNullOrWhiteSpace(request.RegistrationStatus) ? "Открыта" : request.RegistrationStatus.Trim();
        raceEvent.Intro = request.Intro?.Trim() ?? string.Empty;
        raceEvent.Stages = request.Stages?.Select(stage => new RaceEventStage
        {
            Id = $"stage-{Guid.NewGuid():N}"[..14],
            Title = stage.Title.Trim(),
            Date = stage.Date.Trim(),
            Intro = stage.Intro?.Trim() ?? string.Empty,
            RegistrationStatus = string.IsNullOrWhiteSpace(stage.RegistrationStatus) ? "Открыта" : stage.RegistrationStatus.Trim(),
            BannerImage = stage.BannerImage?.Trim() ?? string.Empty
        }).ToList() ?? [];
        raceEvent.PersonalStandings = request.PersonalStandings?.ToList() ?? [];
        raceEvent.TeamStandings = request.TeamStandings?.ToList() ?? [];

        await events.UpdateAsync(raceEvent, cancellationToken);
        return new EventResponse("Событие обновлено.", raceEvent);
    }

    public async Task<EventResponse> DeleteAsync(string eventId, string organizerUserId, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        await RequireOrganizerAsync(organizerUserId, raceEvent, cancellationToken);
        await events.DeleteAsync(eventId, cancellationToken);
        return new EventResponse("Событие удалено.", raceEvent);
    }

    public async Task RequireOrganizerAccessAsync(string eventId, string organizerUserId, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        await RequireOrganizerAsync(organizerUserId, raceEvent, cancellationToken);
    }

    public async Task<EventResponse> RegisterParticipantAsync(string eventId, EventRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        if (raceEvent.RegistrationStatus.Equals("Закрыта", StringComparison.OrdinalIgnoreCase) ||
            raceEvent.RegistrationStatus.Equals("Недоступно", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Регистрация на событие закрыта.");
        }

        if (raceEvent.ParticipantLimit > 0 && raceEvent.Participants.Count >= raceEvent.ParticipantLimit)
        {
            throw new InvalidOperationException("Лимит участников исчерпан.");
        }

if (string.IsNullOrWhiteSpace(request.FullName)) throw new InvalidOperationException("Укажите фамилию и имя пилота.");
if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) throw new InvalidOperationException("Укажите корректную почту пилота.");
if (string.IsNullOrWhiteSpace(request.Car)) throw new InvalidOperationException("Укажите автомобиль пилота.");
PhoneNumberValidator.EnsureValid(request.Phone);

var duplicate = raceEvent.Participants.Any(participant =>
    (!string.IsNullOrWhiteSpace(request.UserId) && string.Equals(participant.UserId, request.UserId, StringComparison.OrdinalIgnoreCase)) ||
    string.Equals(participant.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase));
if (duplicate)
{
    throw new InvalidOperationException("Пилот уже зарегистрирован на это событие.");
}
        var registrationUser = !string.IsNullOrWhiteSpace(request.UserId) ? await users.GetByIdAsync(request.UserId, cancellationToken) : null;
        var membershipTeam = registrationUser?.Profile.TeamMemberships.FirstOrDefault(item => item.TeamType == "Racing")?.TeamName;
        var ownTeam = registrationUser?.Profile.RacingTeamName;
        var participant = new EventParticipant
        {
            Id = $"participant-{Guid.NewGuid():N}"[..20],
            UserId = request.UserId?.Trim() ?? string.Empty,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            Car = request.Car.Trim(),
            TeamName = !string.IsNullOrWhiteSpace(ownTeam) ? ownTeam : (!string.IsNullOrWhiteSpace(membershipTeam) ? membershipTeam : "Нету")
            ,DriverNumber = registrationUser?.Profile.DriverNumber ?? string.Empty
            ,QualificationTimeSeconds = request.QualificationTimeSeconds
            ,ClassName = request.ClassName?.Trim() ?? string.Empty
        };

        raceEvent.Participants.Add(participant);
        await events.UpdateAsync(raceEvent, cancellationToken);

        if (!string.IsNullOrWhiteSpace(participant.UserId))
        {
            var user = await users.GetByIdAsync(participant.UserId, cancellationToken);
            if (user is not null)
            {
                user.Applications.Add(new UserApplication
                {
                    Id = $"application-{Guid.NewGuid():N}"[..20],
                    EventName = raceEvent.Title,
                    Date = raceEvent.Date,
                    Location = raceEvent.Track,
                    Discipline = raceEvent.Discipline,
                    Status = "Зарегистрирован"
                });
                await users.UpdateAsync(user, cancellationToken);
            }
        }

        return new EventResponse("Участник зарегистрирован.", raceEvent);
    }

    public async Task<EventResponse> RejectRegistrationAsync(string eventId, string participantId, RejectEventRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        await RequireOrganizerAsync(request.OrganizerUserId, raceEvent, cancellationToken);

        var participant = raceEvent.Participants.FirstOrDefault(item =>
            string.Equals(item.Id, participantId, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrWhiteSpace(request.Email) && string.Equals(item.Email, request.Email, StringComparison.OrdinalIgnoreCase)));

        if (participant is null)
        {
            throw new InvalidOperationException("Заявка пилота не найдена.");
        }

        participant.Status = "Отклонено организатором";
        await events.UpdateAsync(raceEvent, cancellationToken);
        if (!string.IsNullOrWhiteSpace(participant.UserId))
        {
            var user = await users.GetByIdAsync(participant.UserId, cancellationToken);
            var application = user?.Applications.FirstOrDefault(item =>
                string.Equals(item.EventName, raceEvent.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Date, raceEvent.Date, StringComparison.OrdinalIgnoreCase));
            if (user is not null && application is not null)
            {
                application.Status = "Отклонено организатором";
                await users.UpdateAsync(user, cancellationToken);
            }
        }
        return new EventResponse("Заявка пилота отклонена.", raceEvent);
    }

    public async Task<EventResponse> CancelRegistrationAsync(string eventId, string participantId, CancelEventRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        var raceEvent = await GetByIdAsync(eventId, cancellationToken);
        var participant = raceEvent.Participants.FirstOrDefault(item =>
            string.Equals(item.Id, participantId, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(request.UserId) || string.Equals(item.UserId, request.UserId, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(request.Email) || string.Equals(item.Email, request.Email, StringComparison.OrdinalIgnoreCase)));

        if (participant is null)
        {
            throw new InvalidOperationException("Заявка не найдена или принадлежит другому пользователю.");
        }

        participant.Status = "Отклонил участие";
        await events.UpdateAsync(raceEvent, cancellationToken);

        if (!string.IsNullOrWhiteSpace(participant.UserId))
        {
            var user = await users.GetByIdAsync(participant.UserId, cancellationToken);
            var application = user?.Applications.FirstOrDefault(item =>
                string.Equals(item.EventName, raceEvent.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Date, raceEvent.Date, StringComparison.OrdinalIgnoreCase));
            if (user is not null && application is not null)
            {
                application.Status = "Отклонил участие";
                await users.UpdateAsync(user, cancellationToken);
            }
        }

        return new EventResponse("Заявка снята.", raceEvent);
    }

    private async Task RequireOrganizerAsync(string organizerUserId, RaceEvent raceEvent, CancellationToken cancellationToken)
    {
        var organizer = await users.GetByIdAsync(organizerUserId, cancellationToken);
        if (organizer is null || organizer.Role != "Организатор")
        {
            throw new InvalidOperationException("Изменять события может только организатор.");
        }

        if (!DemoAccess.IsGlobalOrganizer(organizerUserId) &&
            !string.IsNullOrWhiteSpace(raceEvent.OrganizerUserId) &&
            !string.Equals(raceEvent.OrganizerUserId, organizerUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Можно изменять только свои события.");
        }
    }
}
