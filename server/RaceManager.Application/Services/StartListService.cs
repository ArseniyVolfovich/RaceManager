using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class StartListService(IStartListRepository startLists)
{
    public Task<IReadOnlyList<RaceStartListEntry>> GetAsync(string eventId, CancellationToken cancellationToken = default) =>
        startLists.GetByEventAsync(eventId, cancellationToken);

    public async Task<StartListResponse> GenerateAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var entries = await startLists.GenerateAsync(eventId, cancellationToken);
        return new StartListResponse("Стартовый список сформирован.", entries);
    }

    public Task ClearAsync(string eventId, CancellationToken cancellationToken = default) =>
        startLists.ClearAsync(eventId, cancellationToken);

    public async Task<StartListResponse> UpdateAsync(string eventId, UpdateStartListRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Entries.Any(entry => entry.StartNumber <= 0 || entry.StartPosition <= 0))
            throw new InvalidOperationException("Стартовый номер и позиция должны быть больше нуля.");
        if (request.Entries.Select(entry => entry.StartNumber).Distinct().Count() != request.Entries.Count ||
            request.Entries.Select(entry => entry.StartPosition).Distinct().Count() != request.Entries.Count)
            throw new InvalidOperationException("Стартовые номера и позиции не должны повторяться.");

        var entries = await startLists.UpdateAsync(eventId, request.Entries, cancellationToken);
        return new StartListResponse("Стартовый список обновлен.", entries);
    }
}
