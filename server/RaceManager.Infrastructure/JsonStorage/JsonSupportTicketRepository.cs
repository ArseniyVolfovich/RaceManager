using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Infrastructure.JsonStorage;

public sealed class JsonSupportTicketRepository(JsonStorageOptions options) : ISupportTicketRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Tickets.OrderByDescending(item => item.CreatedAtUtc).ToList();
    }

    public async Task<SupportTicket?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Tickets.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            document.Tickets.Add(ticket);
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var index = document.Tickets.FindIndex(item => string.Equals(item.Id, ticket.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Обращение не найдено.");
            }

            document.Tickets[index] = ticket;
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<SupportTicketsDocument> ReadAsync(CancellationToken cancellationToken)
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

    private async Task<SupportTicketsDocument> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        EnsureStorageFile();
        await using var stream = File.OpenRead(options.SupportTicketsPath);
        return await JsonSerializer.DeserializeAsync<SupportTicketsDocument>(stream, JsonOptions, cancellationToken) ?? new SupportTicketsDocument();
    }

    private async Task WriteUnsafeAsync(SupportTicketsDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.SupportTicketsPath)!);
        await using var stream = File.Create(options.SupportTicketsPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private void EnsureStorageFile()
    {
        var directory = Path.GetDirectoryName(options.SupportTicketsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(options.SupportTicketsPath))
        {
            File.WriteAllText(options.SupportTicketsPath, JsonSerializer.Serialize(new SupportTicketsDocument(), JsonOptions));
        }
    }

    private sealed class SupportTicketsDocument
    {
        public List<SupportTicket> Tickets { get; set; } = [];
    }
}
