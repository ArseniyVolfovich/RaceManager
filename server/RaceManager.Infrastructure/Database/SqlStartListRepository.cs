using Microsoft.EntityFrameworkCore;
using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;
using RaceManager.Infrastructure.Database.Models;

namespace RaceManager.Infrastructure.Database;

public sealed class SqlStartListRepository(RaceManagerDbContext dbContext) : IStartListRepository
{
    public async Task<IReadOnlyList<RaceStartListEntry>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await FindEventAsync(eventId, cancellationToken);
        if (eventEntity is null) return [];
        var entries = await Query(eventEntity.EventId).OrderBy(entry => entry.StartPosition).ToListAsync(cancellationToken);
        return entries.Select(entry => ToDomain(entry, eventEntity)).ToList();
    }

    public async Task<IReadOnlyList<RaceStartListEntry>> GenerateAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await FindEventAsync(eventId, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
        var registrations = await dbContext.Registrations
            .Where(registration => registration.EventId == eventEntity.EventId && registration.Status == "Approved")
            .OrderBy(registration => registration.ClassName)
            .ThenBy(registration => registration.QualificationTimeSeconds)
            .ThenBy(registration => registration.RegisteredAt)
            .ToListAsync(cancellationToken);

        var eligibleIds = registrations.Select(registration => registration.RegistrationId).ToHashSet();
        var existing = await dbContext.StartLists.Where(entry => entry.EventId == eventEntity.EventId).ToListAsync(cancellationToken);
        dbContext.StartLists.RemoveRange(existing.Where(entry => !eligibleIds.Contains(entry.RegistrationId)));
        var retained = existing.Where(entry => eligibleIds.Contains(entry.RegistrationId)).ToList();
        var temporaryValue = retained
            .SelectMany(entry => new[] { entry.StartNumber ?? 0, entry.StartPosition ?? 0 })
            .DefaultIfEmpty(0)
            .Max() + 1000;
        foreach (var entry in retained)
        {
            entry.StartNumber = temporaryValue;
            entry.StartPosition = temporaryValue;
            temporaryValue++;
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        var usedNumbers = new HashSet<int>();
        var nextNumber = 1;
        for (var index = 0; index < registrations.Count; index++)
        {
            var registration = registrations[index];
            var entry = existing.FirstOrDefault(item => item.RegistrationId == registration.RegistrationId);
            if (entry is null)
            {
                entry = new StartListEntity { EventId = eventEntity.EventId, RegistrationId = registration.RegistrationId };
                dbContext.StartLists.Add(entry);
            }

            var preferred = int.TryParse(registration.DriverNumber, out var driverNumber) && driverNumber > 0 ? driverNumber : 0;
            while (usedNumbers.Contains(nextNumber)) nextNumber++;
            entry.StartNumber = preferred > 0 && usedNumbers.Add(preferred) ? preferred : nextNumber;
            usedNumbers.Add(entry.StartNumber.Value);
            entry.StartPosition = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByEventAsync(eventId, cancellationToken);
    }

    public async Task ClearAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var eventEntity = await FindEventAsync(eventId, cancellationToken);
        if (eventEntity is null) return;
        await dbContext.StartLists
            .Where(entry => entry.EventId == eventEntity.EventId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RaceStartListEntry>> UpdateAsync(string eventId, IReadOnlyList<UpdateStartListEntryRequest> entries, CancellationToken cancellationToken = default)
    {
        var eventEntity = await FindEventAsync(eventId, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
        var stored = await dbContext.StartLists.Where(entry => entry.EventId == eventEntity.EventId).ToListAsync(cancellationToken);

        if (stored.Count != entries.Count ||
            !stored.Select(entry => entry.RegistrationId).OrderBy(id => id)
                .SequenceEqual(entries.Select(entry => entry.RegistrationId).OrderBy(id => id)))
        {
            throw new InvalidOperationException("Передан неполный или некорректный состав стартового списка.");
        }

        var temporaryValue = stored
            .SelectMany(entry => new[] { entry.StartNumber ?? 0, entry.StartPosition ?? 0 })
            .Concat(entries.SelectMany(entry => new[] { entry.StartNumber, entry.StartPosition }))
            .DefaultIfEmpty(0)
            .Max() + 1000;
        foreach (var entry in stored)
        {
            entry.StartNumber = temporaryValue;
            entry.StartPosition = temporaryValue;
            temporaryValue++;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var request in entries)
        {
            var entry = stored.FirstOrDefault(item => item.RegistrationId == request.RegistrationId)
                ?? throw new InvalidOperationException("Участник отсутствует в стартовом списке.");
            entry.StartNumber = request.StartNumber;
            entry.StartPosition = request.StartPosition;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByEventAsync(eventId, cancellationToken);
    }

    private IQueryable<StartListEntity> Query(int eventId) => dbContext.StartLists
        .Include(entry => entry.Registration)
        .Where(entry => entry.EventId == eventId);

    private Task<EventEntity?> FindEventAsync(string eventId, CancellationToken cancellationToken) =>
        dbContext.Events.FirstOrDefaultAsync(entity => entity.ExternalId == eventId || entity.EventId.ToString() == eventId, cancellationToken);

    private static RaceStartListEntry ToDomain(StartListEntity entry, EventEntity eventEntity) => new()
    {
        Id = entry.StartListId,
        EventId = eventEntity.ExternalId ?? eventEntity.EventId.ToString(),
        RegistrationId = entry.RegistrationId,
        StartNumber = entry.StartNumber ?? 0,
        StartPosition = entry.StartPosition ?? 0,
        DriverName = entry.Registration?.FullName ?? string.Empty,
        DriverNumber = entry.Registration?.DriverNumber ?? string.Empty,
        CarName = entry.Registration?.ManualCarName ?? string.Empty,
        TeamName = string.IsNullOrWhiteSpace(entry.Registration?.TeamName) ? "Нету" : entry.Registration.TeamName,
        ClassName = entry.Registration?.ClassName ?? string.Empty
    };
}
