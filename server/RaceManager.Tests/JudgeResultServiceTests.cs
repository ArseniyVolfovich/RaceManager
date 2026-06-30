using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Security;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;

namespace RaceManager.Tests;

public sealed class JudgeResultServiceTests
{
    [Fact]
    public async Task UpsertAsync_AssignedJudge_CalculatesTimingAndPoints()
    {
        var context = CreateContext(assigned: true);
        var response = await context.Service.UpsertAsync(new UpsertRaceResultRequest(
            EventId: EventId,
            StageId: "stage-1",
            ParticipantId: "participant-1",
            DriverName: "Иван Иванов",
            Position: 1,
            LapTime: null,
            BestLap: null,
            Points: null,
            Status: "Финишировал",
            JudgeUserId: JudgeId,
            Lap1Ms: 61_000,
            Lap2Ms: 60_500,
            Lap3Ms: 62_000,
            PenaltyMs: 2_500));

        Assert.Equal(63_000, response.Result.FinalTimeMs);
        Assert.Equal("60.500", response.Result.BestLap);
        Assert.Equal("63.000", response.Result.LapTime);
        Assert.Equal(25, response.Result.Points);
        Assert.Single(context.Results.Items);
    }

    [Fact]
    public async Task UpsertAsync_UnassignedJudge_IsRejected()
    {
        var context = CreateContext(assigned: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.UpsertAsync(CreateRequest()));

        Assert.Equal("Судья не назначен на это событие.", error.Message);
        Assert.Empty(context.Results.Items);
    }

    [Fact]
    public async Task UpsertAsync_RecalculatesPositionsFromFinalTime()
    {
        var context = CreateContext(assigned: true);

        var slower = await context.Service.UpsertAsync(new UpsertRaceResultRequest(
            EventId, null, "participant-1", "Медленный пилот", 1, null, null, 25,
            "Финишировал", JudgeId, 64_000, 63_000, 65_000, 0));
        var faster = await context.Service.UpsertAsync(new UpsertRaceResultRequest(
            EventId, null, "participant-2", "Быстрый пилот", 2, null, null, 18,
            "Финишировал", JudgeId, 61_000, 60_000, 62_000, 0));

        Assert.Equal(1, faster.Result.Position);
        Assert.Equal(25, faster.Result.Points);
        Assert.Equal(2, context.Results.Items.Single(item => item.Id == slower.Result.Id).Position);
        Assert.Equal(18, context.Results.Items.Single(item => item.Id == slower.Result.Id).Points);
    }

    [Fact]
    public async Task UpsertAsync_GlobalJudge_CanEditAnyEvent()
    {
        var results = new FakeResultRepository(null);
        var users = new FakeUserRepository(new User { Id = DemoAccess.GlobalJudgeUserId, Role = "Судья" });
        var service = new JudgeResultService(results, users, new FakeEventJudgeRepository(false));

        var response = await service.UpsertAsync(CreateRequest() with { JudgeUserId = DemoAccess.GlobalJudgeUserId });

        Assert.Equal(1, response.Result.Position);
        Assert.Equal(25, response.Result.Points);
    }

    [Fact]
    public async Task AddTimePenaltyAsync_RecalculatesAllPositionsAndPoints()
    {
        var fast = new RaceResult
        {
            Id = ResultId, EventId = EventId, DriverName = "Быстрый пилот", Position = 1, Points = 25,
            Status = "Финишировал", Lap1Ms = 60_000, Lap2Ms = 60_500, Lap3Ms = 61_000,
            FinalTimeMs = 60_000, LapTime = "60.000"
        };
        var slow = new RaceResult
        {
            Id = "result-2", EventId = EventId, DriverName = "Второй пилот", Position = 2, Points = 18,
            Status = "Финишировал", Lap1Ms = 62_000, Lap2Ms = 62_500, Lap3Ms = 63_000,
            FinalTimeMs = 62_000, LapTime = "62.000"
        };
        var context = CreateContext(assigned: true, existingResult: fast);
        context.Results.Items.Add(slow);

        var response = await context.Service.AddPenaltyAsync(ResultId, new AddPenaltyRequest(JudgeId, "Касание конуса", 0, 3_000));

        Assert.Equal(63_000, response.Result.FinalTimeMs);
        Assert.Equal(2, response.Result.Position);
        Assert.Equal(18, response.Result.Points);
        Assert.Equal(1, slow.Position);
        Assert.Equal(25, slow.Points);
    }

    [Fact]
    public async Task AddPenaltyAsync_UnassignedJudge_IsRejectedWithoutMutation()
    {
        var context = CreateContext(assigned: false, existingResult: CreateResult());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.AddPenaltyAsync(ResultId, new AddPenaltyRequest(JudgeId, "Фальстарт", 3)));

        Assert.Equal("Судья не назначен на это событие.", error.Message);
        Assert.Empty(context.Results.Items[0].Penalties);
        Assert.Equal(18, context.Results.Items[0].Points);
        Assert.Equal(0, context.Results.UpdateCount);
    }

    [Fact]
    public async Task DisqualifyAsync_UnassignedJudge_IsRejectedWithoutMutation()
    {
        var context = CreateContext(assigned: false, existingResult: CreateResult());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Service.DisqualifyAsync(ResultId, new DisqualifyRequest(JudgeId, "Опасное вождение")));

        Assert.Equal("Судья не назначен на это событие.", error.Message);
        Assert.Equal("Финишировал", context.Results.Items[0].Status);
        Assert.Equal(18, context.Results.Items[0].Points);
        Assert.Equal(0, context.Results.UpdateCount);
    }

    [Fact]
    public async Task PenaltyAndDisqualification_AssignedJudge_UpdateResult()
    {
        var context = CreateContext(assigned: true, existingResult: CreateResult());

        await context.Service.AddPenaltyAsync(ResultId, new AddPenaltyRequest(JudgeId, "Фальстарт", 3));
        await context.Service.DisqualifyAsync(ResultId, new DisqualifyRequest(JudgeId, "Повторное нарушение"));

        var result = context.Results.Items[0];
        Assert.Equal("Дисквалифицирован", result.Status);
        Assert.Equal(0, result.Points);
        Assert.Equal(2, result.Penalties.Count);
        Assert.Equal(3, context.Results.UpdateCount);
    }

    private const string JudgeId = "judge-1";
    private const string EventId = "event-1";
    private const string ResultId = "result-1";

    private static TestContext CreateContext(bool assigned, RaceResult? existingResult = null)
    {
        var results = new FakeResultRepository(existingResult);
        var users = new FakeUserRepository(new User { Id = JudgeId, Role = "Судья" });
        var eventJudges = new FakeEventJudgeRepository(assigned);
        return new TestContext(new JudgeResultService(results, users, eventJudges), results);
    }

    private static UpsertRaceResultRequest CreateRequest() => new(
        EventId, null, "participant-1", "Иван Иванов", 2, null, null, null,
        "Финишировал", JudgeId, 61_000, 60_500, 62_000, 0);

    private static RaceResult CreateResult() => new()
    {
        Id = ResultId,
        EventId = EventId,
        DriverName = "Иван Иванов",
        Position = 2,
        Points = 18,
        Status = "Финишировал"
    };

    private sealed record TestContext(JudgeResultService Service, FakeResultRepository Results);

    private sealed class FakeResultRepository(RaceResult? initial) : IResultRepository
    {
        public List<RaceResult> Items { get; } = initial is null ? [] : [initial];
        public int UpdateCount { get; private set; }

        public Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RaceResult>>(Items);

        public Task<RaceResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(result => result.Id == id));

        public Task AddAsync(RaceResult result, CancellationToken cancellationToken = default)
        {
            Items.Add(result);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RaceResult result, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository(User judge) : IUserRepository
    {
        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>([judge]);

        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(id == judge.Id ? judge : null);

        public Task<User?> FindByEmailOrLoginAsync(string emailOrLogin, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(null);

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeEventJudgeRepository(bool assigned) : IEventJudgeRepository
    {
        public Task<IReadOnlyList<EventJudgeInfo>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventJudgeInfo>>([]);

        public Task<IReadOnlyList<EventJudgeInfo>> GetByJudgeAsync(string judgeUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventJudgeInfo>>([]);

        public Task AssignAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> IsAssignedAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(assigned && eventId == EventId && judgeUserId == JudgeId);
    }
}
