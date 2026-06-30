using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface IChampionshipRepository
{
    Task<IReadOnlyList<Championship>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Championship?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(Championship championship, CancellationToken cancellationToken = default);
    Task UpdateAsync(Championship championship, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
