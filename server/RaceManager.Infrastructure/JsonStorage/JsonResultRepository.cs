using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Infrastructure.JsonStorage;

public sealed class JsonResultRepository(JsonStorageOptions options) : IResultRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Results.OrderBy(item => item.Position).ToList();
    }

    public async Task<RaceResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Results.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(RaceResult result, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var index = document.Results.FindIndex(item => SameParticipant(item, result));
            if (index >= 0)
            {
                result.Id = document.Results[index].Id;
                document.Results[index] = result;
            }
            else
            {
                document.Results.Add(result);
            }
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(RaceResult result, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var index = document.Results.FindIndex(item => string.Equals(item.Id, result.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Результат не найден.");
            }

            document.Results[index] = result;
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ResultsDocument> ReadAsync(CancellationToken cancellationToken)
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

    private async Task<ResultsDocument> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        EnsureStorageFile();
        await using var stream = File.OpenRead(options.ResultsPath);
        var document = await JsonSerializer.DeserializeAsync<ResultsDocument>(stream, JsonOptions, cancellationToken) ?? new ResultsDocument();
        document.Results = document.Results
            .GroupBy(ResultKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => item.UpdatedAtUtc).First())
            .ToList();
        return document;
    }

    private static bool SameParticipant(RaceResult left, RaceResult right) =>
        string.Equals(ResultKey(left), ResultKey(right), StringComparison.OrdinalIgnoreCase);

    private static string ResultKey(RaceResult result)
    {
        var participant = !string.IsNullOrWhiteSpace(result.ParticipantId)
            ? result.ParticipantId.Trim()
            : !string.IsNullOrWhiteSpace(result.DriverName)
                ? result.DriverName.Trim()
                : result.Id.Trim();
        return $"{result.EventId.Trim()}\u001f{participant}";
    }

    private async Task WriteUnsafeAsync(ResultsDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.ResultsPath)!);
        await using var stream = File.Create(options.ResultsPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private void EnsureStorageFile()
    {
        var directory = Path.GetDirectoryName(options.ResultsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(options.ResultsPath))
        {
            File.WriteAllText(options.ResultsPath, JsonSerializer.Serialize(new ResultsDocument(), JsonOptions));
        }
    }

    private sealed class ResultsDocument
    {
        public List<RaceResult> Results { get; set; } = [];
    }
}
