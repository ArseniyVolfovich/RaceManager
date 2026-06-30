using System.Text.Json;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Infrastructure.JsonStorage;

public sealed class JsonUserRepository(JsonStorageOptions options) : IUserRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Users;
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Users.FirstOrDefault(user => string.Equals(user.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<User?> FindByEmailOrLoginAsync(string emailOrLogin, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Users.FirstOrDefault(user =>
            string.Equals(user.Email, emailOrLogin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Login, emailOrLogin, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Users.Any(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default)
    {
        var document = await ReadAsync(cancellationToken);
        return document.Users.Any(user => string.Equals(user.Login, login, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default)
    {
        var value = NormalizePhone(phone);
        var document = await ReadAsync(cancellationToken);
        return document.Users.Any(user => NormalizePhone(user.Profile.Phone) == value);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            document.Users.Add(user);
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var document = await ReadUnsafeAsync(cancellationToken);
            var index = document.Users.FindIndex(item => string.Equals(item.Id, user.Id, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new InvalidOperationException("Пользователь не найден.");
            }

            document.Users[index] = user;
            await WriteUnsafeAsync(document, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<UsersDocument> ReadAsync(CancellationToken cancellationToken)
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

    private async Task<UsersDocument> ReadUnsafeAsync(CancellationToken cancellationToken)
    {
        EnsureStorageFile();
        await using var stream = File.OpenRead(options.UsersPath);
        return await JsonSerializer.DeserializeAsync<UsersDocument>(stream, JsonOptions, cancellationToken) ?? new UsersDocument();
    }

    private async Task WriteUnsafeAsync(UsersDocument document, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(options.UsersPath)!);
        await using var stream = File.Create(options.UsersPath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private void EnsureStorageFile()
    {
        var directory = Path.GetDirectoryName(options.UsersPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(options.UsersPath))
        {
            File.WriteAllText(options.UsersPath, """
            {
              "users": []
            }
            """);
        }
    }

    private static string NormalizePhone(string phone)
    {
        return (phone ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
    }

    private sealed class UsersDocument
    {
        public List<User> Users { get; set; } = [];
    }
}
