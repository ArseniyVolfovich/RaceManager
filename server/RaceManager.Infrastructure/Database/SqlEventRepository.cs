using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlEventRepository(RaceManagerDbContext dbContext) : IEventRepository
{
    public async Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await QueryEvents().OrderBy(x => x.DateStart).ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<RaceEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryEvents().FirstOrDefaultAsync(x => x.ExternalId == id || x.EventId.ToString() == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default)
    {
        var entity = await FindEventAsync(raceEvent.Id, cancellationToken);
        if (entity is not null)
        {
            await UpdateEntityAsync(entity, raceEvent, cancellationToken);
            return;
        }

        entity = new EventEntity
        {
            ExternalId = string.IsNullOrWhiteSpace(raceEvent.Id) ? $"event-{Guid.NewGuid():N}"[..14] : raceEvent.Id,
            CreatedAt = DateTime.UtcNow
        };
        await UpdateEntityAsync(entity, raceEvent, cancellationToken);
        dbContext.Events.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        raceEvent.Id = entity.ExternalId ?? entity.EventId.ToString();
    }

    public async Task UpdateAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default)
    {
        var entity = await FindEventAsync(raceEvent.Id, cancellationToken);
        if (entity is null)
        {
            await AddAsync(raceEvent, cancellationToken);
            return;
        }

        await UpdateEntityAsync(entity, raceEvent, cancellationToken);
        await SyncParticipantsAsync(entity, raceEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.ExternalId == id || item.EventId.ToString() == id,
                cancellationToken);
        if (entity is null) return;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var resultIds = dbContext.Results
            .Where(result => result.EventId == entity.EventId)
            .Select(result => result.ResultId);

        await dbContext.Penalties
            .Where(penalty => resultIds.Contains(penalty.ResultId))
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Results
            .Where(result => result.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.StartLists
            .Where(entry => entry.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.EventJudges
            .Where(judge => judge.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.MediaItems
            .Where(media => media.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Registrations
            .Where(registration => registration.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Events
            .Where(item => item.EventId == entity.EventId)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<EventEntity> QueryEvents()
    {
        return dbContext.Events
            .Include(x => x.Organizer)
            .Include(x => x.Discipline)
            .Include(x => x.Track)
            .Include(x => x.Registrations).ThenInclude(x => x.User)
            .AsSplitQuery();
    }

    private Task<EventEntity?> FindEventAsync(string id, CancellationToken cancellationToken)
    {
        return QueryEvents().FirstOrDefaultAsync(x => x.ExternalId == id || x.EventId.ToString() == id, cancellationToken);
    }

    private async Task UpdateEntityAsync(EventEntity entity, RaceEvent raceEvent, CancellationToken cancellationToken)
    {
        entity.OrganizerId = await ResolveOrganizerIdAsync(raceEvent.OrganizerUserId, cancellationToken);
        entity.DisciplineId = (await GetOrCreateDisciplineAsync(raceEvent.Discipline, cancellationToken)).DisciplineId;
        entity.ChampionshipId = await ResolveChampionshipIdAsync(entity, raceEvent, cancellationToken);
        entity.TrackId = (await GetOrCreateTrackAsync(raceEvent.Track, cancellationToken)).TrackId;
        entity.Name = raceEvent.Title.Trim();
        entity.Type = ToSqlEventType(raceEvent.Type);
        entity.Description = raceEvent.Intro;
        entity.DateStart = ParseDate(raceEvent.Date);
        entity.RegistrationStatus = ToSqlRegistrationStatus(raceEvent.RegistrationStatus);
        entity.MaxParticipants = raceEvent.ParticipantLimit > 0 ? raceEvent.ParticipantLimit : null;
        entity.LapsCount = raceEvent.Laps;
        entity.DistanceMeters = int.TryParse(raceEvent.Distance, out var distance) ? distance : null;
        entity.TrackConfigImageUrl = raceEvent.TrackConfigImage;
        entity.BannerUrl = raceEvent.BannerImage;
        entity.CalendarBannerUrl = raceEvent.CalendarBannerImage;
        entity.OrganizerName = raceEvent.OrganizerName;
        entity.OrganizerColor = raceEvent.OrganizerColor;
        entity.OrganizerLogoUrl = raceEvent.OrganizerLogo;
        entity.StagesJson = JsonSerializer.Serialize(raceEvent.Stages);
        entity.Status = ToSqlStatus(raceEvent.RegistrationStatus, raceEvent.Title);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<int?> ResolveChampionshipIdAsync(
        EventEntity eventEntity,
        RaceEvent raceEvent,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(raceEvent.ChampionshipId, out var requestedChampionshipId))
        {
            var exists = await dbContext.Championships.AnyAsync(item => item.ChampionshipId == requestedChampionshipId, cancellationToken);
            if (!exists) throw new InvalidOperationException("Чемпионат для этапа не найден.");
            return requestedChampionshipId;
        }
        if (!raceEvent.Type.Equals("Чемпионат", StringComparison.OrdinalIgnoreCase)) return null;

        var seasonYear = ParseDate(raceEvent.Date).Year;
        var championship = eventEntity.ChampionshipId is int championshipId
            ? await dbContext.Championships.FirstOrDefaultAsync(item => item.ChampionshipId == championshipId, cancellationToken)
            : null;
        championship ??= await dbContext.Championships.FirstOrDefaultAsync(item =>
            item.OrganizerId == eventEntity.OrganizerId &&
            item.Name == raceEvent.Title &&
            item.SeasonYear == seasonYear,
            cancellationToken);

        if (championship is null)
        {
            championship = new ChampionshipEntity { CreatedAt = DateTime.UtcNow };
            dbContext.Championships.Add(championship);
        }

        championship.OrganizerId = eventEntity.OrganizerId;
        championship.DisciplineId = eventEntity.DisciplineId;
        championship.Name = raceEvent.Title.Trim();
        championship.Description = raceEvent.Intro;
        championship.SeasonYear = seasonYear;
        championship.BannerUrl = raceEvent.BannerImage;
        championship.Status = "Active";
        championship.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return championship.ChampionshipId;
    }

    private async Task SyncParticipantsAsync(EventEntity entity, RaceEvent raceEvent, CancellationToken cancellationToken)
    {
        foreach (var participant in raceEvent.Participants)
        {
            var user = await GetOrCreateUserAsync(participant, cancellationToken);
            var registration = entity.Registrations.FirstOrDefault(x => x.UserId == user.UserId || x.Email == participant.Email);
            if (registration is null)
            {
                registration = new RegistrationEntity
                {
                    EventId = entity.EventId,
                    UserId = user.UserId,
                    RegisteredAt = participant.RegisteredAtUtc == default ? DateTime.UtcNow : participant.RegisteredAtUtc
                };
                entity.Registrations.Add(registration);
            }

            registration.FullName = participant.FullName;
            registration.Email = participant.Email;
            registration.Phone = participant.Phone;
            registration.ManualCarName = participant.Car;
            registration.TeamName = participant.TeamName;
            registration.DriverNumber = participant.DriverNumber;
            registration.QualificationTimeSeconds = participant.QualificationTimeSeconds;
            registration.ClassName = participant.ClassName;
            registration.Status = participant.Status switch
            {
                "Отклонил участие" => "Withdrawn",
                "Отклонено организатором" => "Declined",
                _ => "Approved"
            };
            registration.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<int> ResolveOrganizerIdAsync(string externalId, CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.ExternalId == externalId || x.UserId.ToString() == externalId, cancellationToken)
            ?? await dbContext.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Role != null && x.Role.Name == "Organizer", cancellationToken)
            ?? await GetOrCreateSystemUserAsync("organizer@racemanager.local", "organizer", "Organizer", cancellationToken);
        return organizer.UserId;
    }

    private async Task<DisciplineEntity> GetOrCreateDisciplineAsync(string displayName, CancellationToken cancellationToken)
    {
        var name = displayName.ToLowerInvariant() switch
        {
            var value when value.Contains("drag") || value.Contains("дрэг") => "DragRacing",
            var value when value.Contains("time") || value.Contains("тайм") => "TimeAttack",
            _ => "Drift"
        };
        var discipline = await dbContext.Disciplines.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
        if (discipline is not null) return discipline;
        discipline = new DisciplineEntity { Name = name, DisplayName = displayName };
        dbContext.Disciplines.Add(discipline);
        await dbContext.SaveChangesAsync(cancellationToken);
        return discipline;
    }

    private async Task<TrackEntity> GetOrCreateTrackAsync(string name, CancellationToken cancellationToken)
    {
        var value = string.IsNullOrWhiteSpace(name) ? "RaceManager" : name.Trim();
        var track = await dbContext.Tracks.FirstOrDefaultAsync(x => x.Name == value, cancellationToken);
        if (track is not null) return track;
        track = new TrackEntity { Name = value, Location = value };
        dbContext.Tracks.Add(track);
        await dbContext.SaveChangesAsync(cancellationToken);
        return track;
    }

    private async Task<UserEntity> GetOrCreateUserAsync(EventParticipant participant, CancellationToken cancellationToken)
    {
        var email = string.IsNullOrWhiteSpace(participant.Email) ? $"participant-{Guid.NewGuid():N}@racemanager.local" : participant.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.ExternalId == participant.UserId || x.Email == email, cancellationToken);
        if (user is not null) return user;
        var login = email.Split('@')[0];
        return await GetOrCreateSystemUserAsync(email, login, "User", cancellationToken, participant.UserId);
    }

    private async Task<UserEntity> GetOrCreateSystemUserAsync(string email, string login, string roleName, CancellationToken cancellationToken, string? externalId = null)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);
        if (role is null)
        {
            role = new RoleEntity { Name = roleName, DisplayName = roleName == "Organizer" ? "Организатор" : "Пользователь" };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var uniqueLogin = login;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(x => x.Login == uniqueLogin, cancellationToken)) uniqueLogin = $"{login}{suffix++}";

        var user = new UserEntity
        {
            ExternalId = string.IsNullOrWhiteSpace(externalId) ? $"user-{Guid.NewGuid():N}"[..13] : externalId,
            RoleId = role.RoleId,
            Login = uniqueLogin,
            Email = email,
            PasswordHash = "RaceManager123",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static RaceEvent ToDomain(EventEntity entity)
    {
        return new RaceEvent
        {
            Id = entity.ExternalId ?? entity.EventId.ToString(),
            ChampionshipId = entity.ChampionshipId?.ToString() ?? string.Empty,
            OrganizerUserId = entity.Organizer?.ExternalId ?? entity.OrganizerId.ToString(),
            Type = FromSqlEventType(entity.Type),
            Title = entity.Name,
            Discipline = entity.Discipline?.DisplayName ?? entity.Discipline?.Name ?? string.Empty,
            ParticipantLimit = entity.MaxParticipants ?? 0,
            Track = entity.Track?.Name ?? string.Empty,
            Distance = entity.DistanceMeters?.ToString() ?? string.Empty,
            Laps = entity.LapsCount,
            TrackConfigImage = entity.TrackConfigImageUrl ?? string.Empty,
            BannerImage = entity.BannerUrl ?? string.Empty,
            CalendarBannerImage = entity.CalendarBannerUrl ?? string.Empty,
            OrganizerName = entity.OrganizerName ?? string.Empty,
            OrganizerColor = entity.OrganizerColor ?? "#e10600",
            OrganizerLogo = entity.OrganizerLogoUrl ?? string.Empty,
            Date = entity.DateStart.ToString("yyyy-MM-dd"),
            RegistrationStatus = FromSqlRegistrationStatus(entity.RegistrationStatus),
            Intro = entity.Description ?? string.Empty,
            Stages = DeserializeStages(entity.StagesJson),
            Participants = entity.Registrations.Select(ToParticipant).ToList()
        };
    }

    private static List<RaceEventStage> DeserializeStages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<List<RaceEventStage>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static EventParticipant ToParticipant(RegistrationEntity entity)
    {
        return new EventParticipant
        {
            Id = entity.RegistrationId.ToString(),
            UserId = entity.User?.ExternalId ?? entity.UserId.ToString(),
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone ?? string.Empty,
            Car = entity.ManualCarName ?? entity.Car?.Name ?? string.Empty,
            TeamName = string.IsNullOrWhiteSpace(entity.TeamName) ? "Нету" : entity.TeamName,
            DriverNumber = entity.DriverNumber ?? string.Empty,
            QualificationTimeSeconds = entity.QualificationTimeSeconds,
            ClassName = entity.ClassName ?? string.Empty,
            Status = entity.Status switch
            {
                "Withdrawn" => "Отклонил участие",
                "Declined" => "Отклонено организатором",
                _ => "Зарегистрирован"
            },
            RegisteredAtUtc = entity.RegisteredAt
        };
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParse(value, out var date) ? date : DateTime.UtcNow.Date;
    }

    private static string ToSqlEventType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "чемпионат" or "championship" => "ChampionshipStage",
            "тренировка" or "training" => "Training",
            _ => "TrackDay"
        };
    }

    private static string FromSqlEventType(string value)
    {
        return value switch
        {
            "ChampionshipStage" => "Чемпионат",
            "Training" => "Тренировка",
            _ => "Трек-день"
        };
    }

    private static string ToSqlRegistrationStatus(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "регистрация открыта" or "открыта" or "open" => "Open",
            "скоро" or "comingsoon" => "ComingSoon",
            "завершено" or "completed" => "Completed",
            _ => "Closed"
        };
    }

    private static string FromSqlRegistrationStatus(string value)
    {
        return value switch
        {
            "Open" => "Регистрация открыта",
            "ComingSoon" => "Скоро",
            "Completed" => "Завершено",
            _ => "Регистрация закрыта"
        };
    }

    private static string ToSqlStatus(string registrationStatus, string title)
    {
        if (registrationStatus.Contains("заверш", StringComparison.OrdinalIgnoreCase)) return "Completed";
        if (registrationStatus.Contains("закры", StringComparison.OrdinalIgnoreCase)) return "Published";
        return "Active";
    }
}
