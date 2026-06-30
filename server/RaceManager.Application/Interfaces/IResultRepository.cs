using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface IResultRepository
{
    Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RaceResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(RaceResult result, CancellationToken cancellationToken = default);
    Task UpdateAsync(RaceResult result, CancellationToken cancellationToken = default);
}
