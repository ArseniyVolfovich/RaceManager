using Microsoft.EntityFrameworkCore;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlEventJudgeRepository(RaceManagerDbContext dbContext) : IEventJudgeRepository
{
    public async Task<IReadOnlyList<EventJudgeInfo>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var eventKey = await ResolveEventIdAsync(eventId, cancellationToken);
        if (eventKey is null) return [];

        var rows = await Query().Where(x => x.EventId == eventKey.Value).OrderBy(x => x.JudgeUser!.Login).ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<EventJudgeInfo>> GetByJudgeAsync(string judgeUserId, CancellationToken cancellationToken = default)
    {
        var userKey = await ResolveUserIdAsync(judgeUserId, cancellationToken);
        if (userKey is null) return [];

        var rows = await Query().Where(x => x.JudgeUserId == userKey.Value).OrderByDescending(x => x.AssignedAt).ToListAsync(cancellationToken);
        return rows.Select(ToDomain).ToList();
    }

    public async Task AssignAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default)
    {
        var eventKey = await ResolveEventIdAsync(eventId, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
        var judgeKey = await ResolveUserIdAsync(judgeUserId, cancellationToken) ?? throw new InvalidOperationException("Судья не найден.");
        var exists = await dbContext.EventJudges.AnyAsync(x => x.EventId == eventKey && x.JudgeUserId == judgeKey, cancellationToken);
        if (exists) return;

        dbContext.EventJudges.Add(new EventJudgeEntity
        {
            EventId = eventKey,
            JudgeUserId = judgeKey,
            AssignedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default)
    {
        var eventKey = await ResolveEventIdAsync(eventId, cancellationToken);
        var judgeKey = await ResolveUserIdAsync(judgeUserId, cancellationToken);
        if (eventKey is null || judgeKey is null) return;

        await dbContext.EventJudges
            .Where(x => x.EventId == eventKey.Value && x.JudgeUserId == judgeKey.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> IsAssignedAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default)
    {
        var eventKey = await ResolveEventIdAsync(eventId, cancellationToken);
        var judgeKey = await ResolveUserIdAsync(judgeUserId, cancellationToken);
        if (eventKey is null || judgeKey is null) return false;
        return await dbContext.EventJudges.AnyAsync(x => x.EventId == eventKey.Value && x.JudgeUserId == judgeKey.Value, cancellationToken);
    }

    private IQueryable<EventJudgeEntity> Query() => dbContext.EventJudges
        .Include(x => x.JudgeUser)
        .Include(x => x.Event)
        .ThenInclude(x => x!.Track)
        .AsNoTracking();

    private Task<int?> ResolveEventIdAsync(string eventId, CancellationToken cancellationToken) => dbContext.Events
        .Where(x => x.ExternalId == eventId || x.EventId.ToString() == eventId)
        .Select(x => (int?)x.EventId)
        .FirstOrDefaultAsync(cancellationToken);

    private Task<int?> ResolveUserIdAsync(string userId, CancellationToken cancellationToken) => dbContext.Users
        .Where(x => x.ExternalId == userId || x.UserId.ToString() == userId)
        .Select(x => (int?)x.UserId)
        .FirstOrDefaultAsync(cancellationToken);

    private static EventJudgeInfo ToDomain(EventJudgeEntity entity)
    {
        var user = entity.JudgeUser;
        var fullName = string.Join(" ", new[] { user?.LastName, user?.FirstName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return new EventJudgeInfo
        {
            EventId = entity.Event?.ExternalId ?? entity.EventId.ToString(),
            EventName = entity.Event?.Name ?? string.Empty,
            EventDate = entity.Event?.DateStart.ToString("yyyy-MM-dd") ?? string.Empty,
            EventTrack = entity.Event?.Track?.Name ?? string.Empty,
            UserId = user?.ExternalId ?? entity.JudgeUserId.ToString(),
            Login = user?.Login ?? string.Empty,
            FullName = string.IsNullOrWhiteSpace(fullName) ? user?.Login ?? string.Empty : fullName,
            Email = user?.Email ?? string.Empty,
            AssignedAtUtc = entity.AssignedAt
        };
    }
}

