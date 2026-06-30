using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Infrastructure.JsonStorage;

public sealed class JsonEventRepository(JsonStorageOptions options) : IEventRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Events.OrderBy(item => item.Date).ToList();
    }

    public async Task<RaceEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Events.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            document.Events.Add(raceEvent);
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var index = document.Events.FindIndex(item => string.Equals(item.Id, raceEvent.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Событие не найдено.");
            }

            document.Events[index] = raceEvent;
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var removed = document.Events.RemoveAll(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
            if (removed == 0)
            {
                throw new InvalidOperationException("Событие не найдено.");
            }

            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<EventsDocument> ReadAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadUnsafeAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<EventsDocument> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        EnsureStorageFile();
        await using var stream = File.OpenRead(options.EventsPath);
        return await JsonSerializer.DeserializeAsync<EventsDocument>(stream, JsonOptions, cancellationToken) ?? new EventsDocument();
    }

    private async Task WriteUnsafeAsync(EventsDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.EventsPath)!);
        await using var stream = File.Create(options.EventsPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private void EnsureStorageFile()
    {
        var directory = Path.GetDirectoryName(options.EventsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(options.EventsPath))
        {
            File.WriteAllText(options.EventsPath, JsonSerializer.Serialize(new EventsDocument(), JsonOptions));
        }
    }

    private sealed class EventsDocument
    {
        public List<RaceEvent> Events { get; set; } = [];
    }
}
