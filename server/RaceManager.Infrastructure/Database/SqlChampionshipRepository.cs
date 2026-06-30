using Microsoft.EntityFrameworkCore;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlChampionshipRepository(RaceManagerDbContext dbContext) : IChampionshipRepository
{
    public async Task<IReadOnlyList<Championship>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await Query().OrderByDescending(item => item.SeasonYear).ThenBy(item => item.Name).ToListAsync(cancellationToken);
        var result = new List<Championship>();
        foreach (var entity in entities) result.Add(await ToDomainAsync(entity, cancellationToken));
        return result;
    }

    public async Task<Championship?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(id, out var championshipId)) return null;
        var entity = await Query().FirstOrDefaultAsync(item => item.ChampionshipId == championshipId, cancellationToken);
        return entity is null ? null : await ToDomainAsync(entity, cancellationToken);
    }

    public async Task AddAsync(Championship championship, CancellationToken cancellationToken = default)
    {
        var entity = new ChampionshipEntity { CreatedAt = DateTime.UtcNow };
        await ApplyAsync(entity, championship, cancellationToken);
        dbContext.Championships.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        championship.Id = entity.ChampionshipId.ToString();
    }

    public async Task UpdateAsync(Championship championship, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(championship.Id, out var id)) throw new InvalidOperationException("Чемпионат не найден.");
        var entity = await dbContext.Championships.FirstOrDefaultAsync(item => item.ChampionshipId == id, cancellationToken)
            ?? throw new InvalidOperationException("Чемпионат не найден.");
        await ApplyAsync(entity, championship, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(id, out var championshipId)) return;
        await dbContext.ChampionshipStandings.Where(item => item.ChampionshipId == championshipId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.TeamStandings.Where(item => item.ChampionshipId == championshipId).ExecuteDeleteAsync(cancellationToken);
        await dbContext.Championships.Where(item => item.ChampionshipId == championshipId).ExecuteDeleteAsync(cancellationToken);
    }

    private IQueryable<ChampionshipEntity> Query() => dbContext.Championships
        .Include(item => item.Organizer)
        .Include(item => item.Discipline)
        .Include(item => item.Events).ThenInclude(item => item.Track)
        .Include(item => item.Events).ThenInclude(item => item.Registrations)
        .AsSplitQuery();

    private async Task ApplyAsync(ChampionshipEntity entity, Championship championship, CancellationToken cancellationToken)
    {
        var organizer = await ResolveOrganizerAsync(championship.OrganizerUserId, cancellationToken);
        var discipline = await dbContext.Disciplines.FirstOrDefaultAsync(item => item.DisplayName == championship.Discipline || item.Name == championship.Discipline, cancellationToken);
        if (discipline is null)
        {
            discipline = new DisciplineEntity { Name = NormalizeDiscipline(championship.Discipline), DisplayName = championship.Discipline };
            dbContext.Disciplines.Add(discipline);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        entity.OrganizerId = organizer.UserId;
        entity.DisciplineId = discipline.DisciplineId;
        entity.Name = championship.Name;
        entity.Description = championship.Description;
        entity.SeasonYear = championship.SeasonYear;
        entity.BannerUrl = championship.BannerUrl;
        entity.RegulationFileUrl = championship.RegulationFileUrl;
        entity.Status = championship.Status;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<UserEntity> ResolveOrganizerAsync(string externalId, CancellationToken cancellationToken)
    {
        var organizer = await dbContext.Users.Include(user => user.Role).FirstOrDefaultAsync(user =>
            user.ExternalId == externalId || user.UserId.ToString() == externalId,
            cancellationToken);
        if (organizer is not null) return organizer;

        var fallback = await dbContext.Users.Include(user => user.Role).FirstOrDefaultAsync(user =>
            user.Role != null && user.Role.Name == "Organizer",
            cancellationToken);
        if (fallback is not null && string.IsNullOrWhiteSpace(externalId)) return fallback;

        return await GetOrCreateSystemOrganizerAsync(externalId, cancellationToken);
    }

    private async Task<UserEntity> GetOrCreateSystemOrganizerAsync(string externalId, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles.FirstOrDefaultAsync(role => role.Name == "Organizer", cancellationToken);
        if (role is null)
        {
            role = new RoleEntity { Name = "Organizer", DisplayName = "Организатор" };
            dbContext.Roles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var login = $"organizer-{suffix}";
        var email = $"organizer-{suffix}@racemanager.local";
        var user = new UserEntity
        {
            ExternalId = string.IsNullOrWhiteSpace(externalId) ? $"user-{Guid.NewGuid():N}"[..13] : externalId,
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

    private async Task<Championship> ToDomainAsync(ChampionshipEntity entity, CancellationToken cancellationToken)
    {
        var driverRows = await (from standing in dbContext.ChampionshipStandings
                                join driver in dbContext.Drivers on standing.DriverId equals driver.DriverId
                                join user in dbContext.Users on driver.UserId equals user.UserId
                                where standing.ChampionshipId == entity.ChampionshipId
                                orderby standing.Position
                                select new StandingRow
                                {
                                    Position = standing.Position,
                                    Name = ((user.LastName ?? "") + " " + (user.FirstName ?? "")).Trim(),
                                    Points = (int)standing.TotalPoints
                                }).ToListAsync(cancellationToken);
        var teamRows = await (from standing in dbContext.TeamStandings
                              join team in dbContext.Teams on standing.TeamId equals team.TeamId
                              where standing.ChampionshipId == entity.ChampionshipId
                              orderby standing.Position
                              select new StandingRow { Position = standing.Position, Name = team.Name, Team = team.Name, Points = (int)standing.TotalPoints })
            .ToListAsync(cancellationToken);

        return new Championship
        {
            Id = entity.ChampionshipId.ToString(),
            OrganizerUserId = entity.Organizer?.ExternalId ?? entity.OrganizerId.ToString(),
            Discipline = entity.Discipline?.DisplayName ?? entity.Discipline?.Name ?? string.Empty,
            Name = entity.Name,
            Description = entity.Description ?? string.Empty,
            SeasonYear = entity.SeasonYear,
            BannerUrl = entity.BannerUrl ?? string.Empty,
            RegulationFileUrl = entity.RegulationFileUrl ?? string.Empty,
            Status = entity.Status,
            CreatedAtUtc = entity.CreatedAt,
            Events = entity.Events.OrderBy(item => item.DateStart).Select(item => new RaceEvent
            {
                Id = item.ExternalId ?? item.EventId.ToString(),
                ChampionshipId = entity.ChampionshipId.ToString(),
                OrganizerUserId = entity.Organizer?.ExternalId ?? entity.OrganizerId.ToString(),
                Type = "Этап чемпионата",
                Title = item.Name,
                Discipline = entity.Discipline?.DisplayName ?? string.Empty,
                Track = item.Track?.Name ?? string.Empty,
                Date = item.DateStart.ToString("yyyy-MM-dd"),
                Laps = item.LapsCount,
                ParticipantLimit = item.MaxParticipants ?? 0,
                RegistrationStatus = item.RegistrationStatus
            }).ToList(),
            DriverStandings = driverRows,
            TeamStandings = teamRows
        };
    }

    private static string NormalizeDiscipline(string value) => value.ToLowerInvariant() switch
    {
        var text when text.Contains("drag") || text.Contains("дрэг") => "DragRacing",
        var text when text.Contains("time") || text.Contains("тайм") => "TimeAttack",
        _ => "Drift"
    };
}
