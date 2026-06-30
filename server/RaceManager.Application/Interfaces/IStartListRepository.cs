using RaceManager.Application.DTOs;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface IStartListRepository
{
    Task<IReadOnlyList<RaceStartListEntry>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RaceStartListEntry>> GenerateAsync(string eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RaceStartListEntry>> UpdateAsync(string eventId, IReadOnlyList<UpdateStartListEntryRequest> entries, CancellationToken cancellationToken = default);
    Task ClearAsync(string eventId, CancellationToken cancellationToken = default);
}
