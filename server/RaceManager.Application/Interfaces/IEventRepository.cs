using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface IEventRepository
{
    Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RaceEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
