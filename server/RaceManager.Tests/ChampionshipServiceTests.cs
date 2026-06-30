using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;

namespace RaceManager.Tests;

public sealed class ChampionshipServiceTests
{
    [Fact]
    public async Task CreateAsync_OrganizerCreatesIndependentChampionship()
    {
        var repository = new ChampionshipRepository();
        var service = new ChampionshipService(repository, new UserRepository(new User { Id = "owner", Role = "Организатор" }));

        var response = await service.CreateAsync("owner", new CreateChampionshipRequest(
            "Кубок 2026", "Тайм-Аттак", 2026, "Сезон", null, null, "Active"));

        Assert.Single(repository.Items);
        Assert.Equal("Кубок 2026", response.Championship.Name);
        Assert.Equal("owner", response.Championship.OrganizerUserId);
        Assert.Empty(response.Championship.Events);
    }

    [Fact]
    public async Task UpdateAsync_OtherOrganizerCannotEditChampionship()
    {
        var championship = new Championship { Id = "1", OrganizerUserId = "owner", Name = "Кубок", Discipline = "Дрифт", SeasonYear = 2026 };
        var service = new ChampionshipService(
            new ChampionshipRepository(championship),
            new UserRepository(new User { Id = "owner", Role = "Организатор" }, new User { Id = "other", Role = "Организатор" }));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync("1", "other",
            new UpdateChampionshipRequest("Чужое изменение", "Дрифт", 2026, null, null, null, "Active")));

        Assert.Equal("Можно изменять только свои чемпионаты.", error.Message);
        Assert.Equal("Кубок", championship.Name);
    }

    private sealed class ChampionshipRepository(params Championship[] values) : IChampionshipRepository
    {
        public List<Championship> Items { get; } = [.. values];
        public Task<IReadOnlyList<Championship>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Championship>>(Items);
        public Task<Championship?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        public Task AddAsync(Championship championship, CancellationToken cancellationToken = default) { championship.Id = (Items.Count + 1).ToString(); Items.Add(championship); return Task.CompletedTask; }
        public Task UpdateAsync(Championship championship, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) { Items.RemoveAll(item => item.Id == id); return Task.CompletedTask; }
    }

    private sealed class UserRepository(params User[] values) : IUserRepository
    {
        private readonly List<User> items = [.. values];
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(items);
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(items.FirstOrDefault(item => item.Id == id));
        public Task<User?> FindByEmailOrLoginAsync(string value, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
