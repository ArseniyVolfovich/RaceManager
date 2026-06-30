using Microsoft.EntityFrameworkCore;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlResultRepository(RaceManagerDbContext dbContext) : IResultRepository
{
    public async Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await QueryResults().OrderBy(x => x.Position).ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<RaceResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await QueryResults().FirstOrDefaultAsync(x => x.ExternalId == id || x.ResultId.ToString() == id, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task AddAsync(RaceResult result, CancellationToken cancellationToken = default)
    {
        var eventEntity = await dbContext.Events
            .Include(x => x.Registrations).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.ExternalId == result.EventId || x.EventId.ToString() == result.EventId, cancellationToken);
        if (eventEntity is null) return;

        var registration = await ResolveRegistrationAsync(eventEntity, result, cancellationToken);
        var entity = await dbContext.Results.FirstOrDefaultAsync(x => x.ExternalId == result.Id || x.RegistrationId == registration.RegistrationId, cancellationToken);
        if (entity is null)
        {
            entity = new ResultEntity
            {
                ExternalId = string.IsNullOrWhiteSpace(result.Id) ? $"result-{Guid.NewGuid():N}"[..15] : result.Id,
                EventId = eventEntity.EventId,
                RegistrationId = registration.RegistrationId,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Results.Add(entity);
        }

        Apply(entity, result, registration);
        await dbContext.SaveChangesAsync(cancellationToken);
        result.Id = entity.ExternalId ?? entity.ResultId.ToString();
        await RecalculateStatisticsAsync(eventEntity.EventId, cancellationToken);
    }

    public async Task UpdateAsync(RaceResult result, CancellationToken cancellationToken = default)
    {
        var entity = await QueryResults().FirstOrDefaultAsync(x => x.ExternalId == result.Id || x.ResultId.ToString() == result.Id, cancellationToken);
        if (entity is null)
        {
            await AddAsync(result, cancellationToken);
            return;
        }

        Apply(entity, result, entity.Registration!);
        await SyncPenaltiesAsync(entity, result, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateStatisticsAsync(entity.EventId, cancellationToken);
    }

    private IQueryable<ResultEntity> QueryResults()
    {
        return dbContext.Results
            .Include(x => x.Event)
            .Include(x => x.Registration).ThenInclude(x => x!.User)
            .Include(x => x.Penalties)
            .AsSplitQuery();
    }

    private async Task<RegistrationEntity> ResolveRegistrationAsync(EventEntity eventEntity, RaceResult result, CancellationToken cancellationToken)
    {
        var registration = eventEntity.Registrations.FirstOrDefault(x =>
            x.RegistrationId.ToString() == result.ParticipantId ||
            (!string.IsNullOrWhiteSpace(result.DriverName) && x.FullName == result.DriverName));
        if (registration is not null) return registration;

        var email = $"result-{Guid.NewGuid():N}@racemanager.local";
        var user = await GetOrCreateUserAsync(email, result.DriverName, cancellationToken);
        registration = new RegistrationEntity
        {
            EventId = eventEntity.EventId,
            UserId = user.UserId,
            FullName = result.DriverName,
            Email = email,
            ManualCarName = string.Empty,
            Status = "Approved",
            RegisteredAt = DateTime.UtcNow
        };
        dbContext.Registrations.Add(registration);
        await dbContext.SaveChangesAsync(cancellationToken);
        return registration;
    }

    private async Task<UserEntity> GetOrCreateUserAsync(string email, string driverName, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is not null) return user;

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == "User", cancellationToken);
        if (role is null)
        {
            role = new RoleEntity { Name = "User", DisplayName = "Пользователь" };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var baseLogin = string.Join("", driverName.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(baseLogin)) baseLogin = "pilot";
        var login = baseLogin;
        var suffix = 1;
        while (await dbContext.Users.AnyAsync(x => x.Login == login, cancellationToken)) login = $"{baseLogin}{suffix++}";

        user = new UserEntity
        {
            ExternalId = $"user-{Guid.NewGuid():N}"[..13],
            RoleId = role.RoleId,
            Login = login,
            Email = email,
            PasswordHash = "RaceManager123",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static void Apply(ResultEntity entity, RaceResult result, RegistrationEntity registration)
    {
        var bestLapMs = BestLap(result.Lap1Ms, result.Lap2Ms, result.Lap3Ms) ?? ParseTimeMs(result.BestLap);
        var penaltyMs = Math.Max(0, result.PenaltyMs ?? 0);
        var finalTimeMs = result.FinalTimeMs ?? (bestLapMs is null ? ParseTimeMs(result.LapTime) : bestLapMs.Value + penaltyMs);
        entity.Position = result.Position > 0 ? result.Position : null;
        entity.BestLapTimeMs = bestLapMs;
        entity.TotalTimeMs = finalTimeMs;
        entity.FinalTimeMs = finalTimeMs;
        entity.Lap1Ms = result.Lap1Ms;
        entity.Lap2Ms = result.Lap2Ms;
        entity.Lap3Ms = result.Lap3Ms;
        entity.PenaltyMs = penaltyMs;
        entity.Points = result.Points;
        entity.Status = ToSqlStatus(result.Status);
        entity.CarName = string.IsNullOrWhiteSpace(result.CarName) ? registration.ManualCarName : result.CarName;
        entity.ClassName = string.IsNullOrWhiteSpace(result.ClassName) ? registration.ClassName : result.ClassName;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private async Task SyncPenaltiesAsync(ResultEntity entity, RaceResult result, CancellationToken cancellationToken)
    {
        foreach (var penalty in result.Penalties)
        {
            if (int.TryParse(penalty.Id, out var penaltyId) && entity.Penalties.Any(item => item.PenaltyId == penaltyId)) continue;
            var duplicate = entity.Penalties.Any(item =>
                item.Reason == penalty.Reason &&
                (int)(item.Points ?? 0) == penalty.Points &&
                (int)Math.Round((item.TimeSeconds ?? 0) * 1000) == penalty.TimeMs);
            if (duplicate) continue;

            var judge = await dbContext.Users.FirstOrDefaultAsync(item =>
                item.ExternalId == penalty.JudgeUserId || item.UserId.ToString() == penalty.JudgeUserId,
                cancellationToken);
            if (judge is null) throw new InvalidOperationException("Судья для штрафа не найден.");
            entity.Penalties.Add(new PenaltyEntity
            {
                JudgeUserId = judge.UserId,
                Reason = penalty.Reason,
                PenaltyType = penalty.TimeMs > 0 ? "TimePenalty" : "PointsPenalty",
                TimeSeconds = penalty.TimeMs > 0 ? penalty.TimeMs / 1000m : null,
                Points = penalty.Points > 0 ? penalty.Points : null,
                CreatedAt = penalty.CreatedAtUtc
            });
        }
    }

    private async Task RecalculateStatisticsAsync(int eventId, CancellationToken cancellationToken)
    {
        var affectedDriverIds = await dbContext.Results
            .Include(x => x.Registration).ThenInclude(x => x!.User).ThenInclude(x => x!.Driver)
            .Where(x => x.EventId == eventId)
            .Where(x => x.Registration!.User!.Driver != null)
            .Select(x => x.Registration!.User!.Driver!.DriverId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allRows = await dbContext.Results
            .Include(x => x.Event)
            .Include(x => x.Registration).ThenInclude(x => x!.User).ThenInclude(x => x!.Driver)
            .Where(x => x.Registration!.User!.Driver != null)
            .ToListAsync(cancellationToken);

        foreach (var driverId in affectedDriverIds)
        {
            var driver = allRows.Select(x => x.Registration!.User!.Driver!).First(x => x.DriverId == driverId);
            var history = allRows.Where(x => x.Registration!.User!.Driver!.DriverId == driverId).ToList();
            driver.TotalPoints = history.Sum(x => x.Points);
            driver.RacesCount = history.Count;
            driver.WinsCount = history.Count(x => x.Position == 1 && x.Status == "Finished");
            driver.PodiumsCount = history.Count(x => x.Position is >= 1 and <= 3 && x.Status == "Finished");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var rankedDrivers = await dbContext.Drivers
            .OrderByDescending(driver => driver.TotalPoints)
            .ThenByDescending(driver => driver.WinsCount)
            .ThenByDescending(driver => driver.PodiumsCount)
            .ThenBy(driver => driver.DriverId)
            .ToListAsync(cancellationToken);
        for (var index = 0; index < rankedDrivers.Count; index++) rankedDrivers[index].RatingPosition = index + 1;

        await RecalculateChampionshipStandingsAsync(allRows, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecalculateChampionshipStandingsAsync(
        IReadOnlyList<ResultEntity> allRows,
        CancellationToken cancellationToken)
    {
        var championshipRows = allRows
            .Where(row => row.Event?.ChampionshipId is not null && row.Registration?.User?.Driver is not null)
            .ToList();
        if (championshipRows.Count == 0) return;

        var teamNames = championshipRows
            .Select(row => row.Registration?.TeamName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name) && !name.Equals("Нету", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var teams = await dbContext.Teams.ToListAsync(cancellationToken);
        foreach (var name in teamNames.Where(name => teams.All(team => !team.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
        {
            var team = new TeamEntity { Name = name!, CreatedAt = DateTime.UtcNow };
            dbContext.Teams.Add(team);
            teams.Add(team);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var championshipGroup in championshipRows.GroupBy(row => row.Event!.ChampionshipId!.Value))
        {
            var championshipId = championshipGroup.Key;
            await dbContext.ChampionshipStandings
                .Where(row => row.ChampionshipId == championshipId)
                .ExecuteDeleteAsync(cancellationToken);
            await dbContext.TeamStandings
                .Where(row => row.ChampionshipId == championshipId)
                .ExecuteDeleteAsync(cancellationToken);

            var drivers = championshipGroup
                .GroupBy(row => row.Registration!.User!.Driver!.DriverId)
                .Select(group => new { DriverId = group.Key, Points = group.Sum(row => row.Points) })
                .OrderByDescending(row => row.Points)
                .ThenBy(row => row.DriverId)
                .ToList();
            dbContext.ChampionshipStandings.AddRange(drivers.Select((row, index) => new ChampionshipStandingEntity
            {
                ChampionshipId = championshipId,
                DriverId = row.DriverId,
                Position = index + 1,
                TotalPoints = row.Points
            }));

            var teamResults = championshipGroup
                .Where(row => !string.IsNullOrWhiteSpace(row.Registration?.TeamName) && !row.Registration!.TeamName!.Equals("Нету", StringComparison.OrdinalIgnoreCase))
                .GroupBy(row => row.Registration!.TeamName!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    Team = teams.First(team => team.Name.Equals(group.Key, StringComparison.OrdinalIgnoreCase)),
                    Points = group.Sum(row => row.Points)
                })
                .OrderByDescending(row => row.Points)
                .ThenBy(row => row.Team.Name)
                .ToList();
            dbContext.TeamStandings.AddRange(teamResults.Select((row, index) => new TeamStandingEntity
            {
                ChampionshipId = championshipId,
                TeamId = row.Team.TeamId,
                Position = index + 1,
                TotalPoints = row.Points
            }));
        }
    }

    private static int? BestLap(params int?[] laps)
    {
        var valid = laps.Where(value => value is > 0).Select(value => value!.Value).ToArray();
        return valid.Length == 0 ? null : valid.Min();
    }

    private static RaceResult ToDomain(ResultEntity entity)
    {
        return new RaceResult
        {
            Id = entity.ExternalId ?? entity.ResultId.ToString(),
            EventId = entity.Event?.ExternalId ?? entity.EventId.ToString(),
            ParticipantId = entity.RegistrationId.ToString(),
            DriverName = entity.Registration?.FullName ?? string.Empty,
            Position = entity.Position ?? 0,
            LapTime = FormatMs(entity.FinalTimeMs ?? entity.TotalTimeMs),
            BestLap = FormatMs(entity.BestLapTimeMs),
            Lap1Ms = entity.Lap1Ms,
            Lap2Ms = entity.Lap2Ms,
            Lap3Ms = entity.Lap3Ms,
            PenaltyMs = entity.PenaltyMs,
            FinalTimeMs = entity.FinalTimeMs,
            ClassName = entity.ClassName ?? string.Empty,
            CarName = entity.CarName ?? entity.Registration?.ManualCarName ?? string.Empty,
            DriverNumber = entity.Registration?.DriverNumber ?? string.Empty,
            Points = (int)entity.Points,
            Status = FromSqlStatus(entity.Status),
            UpdatedAtUtc = entity.UpdatedAt ?? entity.CreatedAt,
            Penalties = entity.Penalties.Select(x => new RacePenalty
            {
                Id = x.PenaltyId.ToString(),
                JudgeUserId = x.JudgeUserId.ToString(),
                Reason = x.Reason,
                Points = (int)(x.Points ?? 0),
                TimeMs = (int)Math.Round((x.TimeSeconds ?? 0) * 1000),
                CreatedAtUtc = x.CreatedAt
            }).ToList()
        };
    }

    private static int? ParseTimeMs(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "—") return null;
        return decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var seconds)
            ? (int)Math.Round(seconds * 1000)
            : null;
    }

    private static string FormatMs(int? ms)
    {
        return ms is null ? string.Empty : (ms.Value / 1000m).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ToSqlStatus(string value)
    {
        return value switch
        {
            "DNF" => "DNF",
            "Дисквалифицирован" or "Дисквалификация" => "DSQ",
            "Ожидает проверки" => "Pending",
            _ => "Finished"
        };
    }

    private static string FromSqlStatus(string value)
    {
        return value switch
        {
            "DNF" => "DNF",
            "DSQ" => "Дисквалифицирован",
            "Pending" => "Ожидает проверки",
            _ => "Финишировал"
        };
    }
}
