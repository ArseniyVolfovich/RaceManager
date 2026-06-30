using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface IEventJudgeRepository
{
    Task<IReadOnlyList<EventJudgeInfo>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventJudgeInfo>> GetByJudgeAsync(string judgeUserId, CancellationToken cancellationToken = default);
    Task AssignAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default);
    Task RemoveAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default);
    Task<bool> IsAssignedAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default);
}

