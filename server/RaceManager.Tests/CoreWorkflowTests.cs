using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Security;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;

namespace RaceManager.Tests;

public sealed class CoreWorkflowTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserWithHashedPasswordAndNormalizedEmail()
    {
        var users = new UserRepository();
        var service = new AuthService(users);

        var response = await service.RegisterAsync(new RegisterRequest(
            "NewPilot", "PILOT@EXAMPLE.COM", "secret12", "Иванов", "Иван", null,
            "+375 29 123 45 67", "2000-01-01", null));

        var user = Assert.Single(users.Items);
        Assert.Equal("pilot@example.com", user.Email);
        Assert.Equal("Пользователь", user.Role);
        Assert.NotEqual("secret12", user.Password);
        Assert.True(PasswordSecurity.Verify("secret12", user.Password, out var upgrade));
        Assert.False(upgrade);
        Assert.Equal(user.Id, response.User.Id);
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        var users = new UserRepository(new User { Id = "existing", Login = "other", Email = "pilot@example.com", Profile = new UserProfile { Phone = "+375 25 111 22 33" } });
        var service = new AuthService(users);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterAsync(new RegisterRequest(
            "NewPilot", "pilot@example.com", "secret12", null, null, null,
            "+375 29 123 45 67", null, null)));

        Assert.Equal("Пользователь с таким email уже существует.", error.Message);
    }

    [Fact]
    public async Task LoginAsync_UpgradesLegacyPlainTextPassword()
    {
        var user = new User { Id = "user-1", Login = "pilot", Email = "pilot@example.com", Password = "legacy-secret" };
        var users = new UserRepository(user);
        var service = new AuthService(users);

        await service.LoginAsync(new LoginRequest("pilot", "legacy-secret"));

        Assert.NotEqual("legacy-secret", user.Password);
        Assert.True(PasswordSecurity.Verify("legacy-secret", user.Password, out _));
        Assert.Equal(1, users.UpdateCount);
    }

    [Fact]
    public async Task EventCrud_OrganizerCanCreateUpdateAndDeleteOwnEvent()
    {
        var organizer = Organizer();
        var users = new UserRepository(organizer);
        var events = new EventRepository();
        var service = new EventService(events, users);

        var created = await service.CreateAsync(CreateEventRequest());
        var updated = await service.UpdateAsync(created.Event.Id, UpdateEventRequest("Обновлённый этап"));
        await service.DeleteAsync(created.Event.Id, organizer.Id);

        Assert.Equal("Обновлённый этап", updated.Event.Title);
        Assert.Empty(events.Items);
        Assert.Equal(1, events.AddCount);
        Assert.Equal(1, events.UpdateCount);
        Assert.Equal(1, events.DeleteCount);
    }

    [Fact]
    public async Task UpdateAsync_OtherOrganizerCannotChangeEvent()
    {
        var owner = Organizer();
        var stranger = Organizer("organizer-2");
        var raceEvent = RaceEvent(owner.Id);
        var service = new EventService(new EventRepository(raceEvent), new UserRepository(owner, stranger));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(raceEvent.Id, UpdateEventRequest("Чужое изменение", stranger.Id)));

        Assert.Equal("Можно изменять только свои события.", error.Message);
        Assert.Equal("Первый этап", raceEvent.Title);
    }

    [Fact]
    public async Task GlobalOrganizer_CanUpdateEventOwnedByAnotherOrganizer()
    {
        var owner = Organizer();
        var globalOrganizer = Organizer(DemoAccess.GlobalOrganizerUserId);
        var raceEvent = RaceEvent(owner.Id);
        var service = new EventService(new EventRepository(raceEvent), new UserRepository(owner, globalOrganizer));

        var response = await service.UpdateAsync(
            raceEvent.Id,
            UpdateEventRequest("Изменено глобальным организатором", globalOrganizer.Id));

        Assert.Equal("Изменено глобальным организатором", response.Event.Title);
    }

    [Fact]
    public async Task RegistrationAndRejection_UpdateEventAndPilotApplication()
    {
        var organizer = Organizer();
        var pilot = Pilot();
        var raceEvent = RaceEvent(organizer.Id);
        var events = new EventRepository(raceEvent);
        var users = new UserRepository(organizer, pilot);
        var service = new EventService(events, users);

        var registration = await service.RegisterParticipantAsync(raceEvent.Id, RegistrationRequest(pilot.Id));
        var participant = Assert.Single(registration.Event.Participants);
        Assert.Equal("77", participant.DriverNumber);
        Assert.Single(pilot.Applications);

        await service.RejectRegistrationAsync(raceEvent.Id, participant.Id, new RejectEventRegistrationRequest(organizer.Id, null));

        Assert.Equal("Отклонено организатором", participant.Status);
        Assert.Equal("Отклонено организатором", pilot.Applications[0].Status);
    }

    [Fact]
    public async Task RegisterParticipantAsync_RejectsDuplicatePilot()
    {
        var organizer = Organizer();
        var pilot = Pilot();
        var raceEvent = RaceEvent(organizer.Id);
        var service = new EventService(new EventRepository(raceEvent), new UserRepository(organizer, pilot));

        await service.RegisterParticipantAsync(raceEvent.Id, RegistrationRequest(pilot.Id));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterParticipantAsync(raceEvent.Id, RegistrationRequest(pilot.Id)));

        Assert.Equal("Пилот уже зарегистрирован на это событие.", error.Message);
        Assert.Single(raceEvent.Participants);
    }

    [Fact]
    public async Task BuildEventReportAsync_UsesSortedResultsAndCalculatedColumns()
    {
        var raceEvent = RaceEvent(OrganizerId);
        var results = new ResultRepository(
            new RaceResult { Id = "r2", EventId = raceEvent.Id, DriverName = "Второй", Position = 2, Points = 18, BestLap = "61.000", LapTime = "62.000", PenaltyMs = 1000 },
            new RaceResult { Id = "r1", EventId = raceEvent.Id, DriverName = "Первый", Position = 1, Points = 25, BestLap = "60.000", LapTime = "60.000", PenaltyMs = 0 });
        var service = new ReportService(new EventRepository(raceEvent), results, new UserRepository());

        var report = await service.BuildEventReportAsync(raceEvent.Id);

        Assert.Equal(2, report.Rows.Count);
        Assert.Equal("Первый", report.Rows[0][2]);
        Assert.Equal("Второй", report.Rows[1][2]);
        Assert.Equal("1.000", report.Rows[1][6]);
    }

    [Fact]
    public async Task BuildChampionshipReportAsync_IncludesOnlyRequestedChampionshipStages()
    {
        var requested = RaceEvent(OrganizerId);
        requested.Type = "Чемпионат";
        requested.Stages =
        [
            new RaceEventStage { Id = "s2", Title = "Этап 2", Date = "2026-07-01", RegistrationStatus = "Скоро" },
            new RaceEventStage { Id = "s1", Title = "Этап 1", Date = "2026-06-01", RegistrationStatus = "Открыта" }
        ];
        var other = RaceEvent(OrganizerId, "event-2");
        other.Type = "Чемпионат";
        other.Title = "Другой чемпионат";
        var service = new ReportService(new EventRepository(requested, other), new ResultRepository(), new UserRepository());

        var report = await service.BuildChampionshipReportAsync(requested.Id);

        Assert.Equal(2, report.Rows.Count);
        Assert.Equal("Этап 1", report.Rows[0][0]);
        Assert.DoesNotContain(report.Rows, row => row[0] == "Другой чемпионат");
    }

    [Fact]
    public async Task StartListUpdateAsync_RejectsDuplicateNumbersAndPositions()
    {
        var service = new StartListService(new StartListRepository());
        var request = new UpdateStartListRequest(
        [
            new UpdateStartListEntryRequest(1, 10, 1),
            new UpdateStartListEntryRequest(2, 10, 2)
        ]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(EventId, request));

        Assert.Equal("Стартовые номера и позиции не должны повторяться.", error.Message);
    }

    private const string OrganizerId = "organizer-1";
    private const string EventId = "event-1";

    private static User Organizer(string id = OrganizerId) => new()
    {
        Id = id,
        Login = id,
        Email = $"{id}@example.com",
        Role = "Организатор",
        Profile = new UserProfile
        {
            OrganizationName = "Race Team",
            OrganizationColor = "#e10600"
        }
    };

    private static User Pilot() => new()
    {
        Id = "pilot-1",
        Login = "pilot",
        Email = "pilot@example.com",
        Role = "Пользователь",
        Profile = new UserProfile
        {
            LastName = "Иванов",
            FirstName = "Иван",
            Phone = "+375 29 123 45 67",
            DriverNumber = "77"
        }
    };

    private static RaceEvent RaceEvent(string organizerId, string id = EventId) => new()
    {
        Id = id,
        OrganizerUserId = organizerId,
        Type = "Трек-день",
        Title = "Первый этап",
        Discipline = "Тайм-Аттак",
        ParticipantLimit = 20,
        Track = "Стайки",
        Date = "2026-06-01",
        RegistrationStatus = "Открыта",
        OrganizerName = "Race Team"
    };

    private static CreateRaceEventRequest CreateEventRequest() => new(
        OrganizerUserId: OrganizerId,
        Type: "Трек-день",
        Title: "Первый этап",
        Discipline: "Тайм-Аттак",
        ParticipantLimit: 20,
        Track: "Стайки",
        Distance: null,
        Laps: 3,
        TrackConfigImage: null,
        BannerImage: null,
        Date: "2026-06-01",
        RegistrationStatus: "Открыта",
        Intro: null,
        Stages: null,
        PersonalStandings: null,
        TeamStandings: null,
        CalendarBannerImage: null,
        OrganizerName: null,
        OrganizerColor: null,
        OrganizerLogo: null);

    private static UpdateRaceEventRequest UpdateEventRequest(string title, string organizerId = OrganizerId) => new(
        OrganizerUserId: organizerId,
        Type: "Трек-день",
        Title: title,
        Discipline: "Тайм-Аттак",
        ParticipantLimit: 20,
        Track: "Стайки",
        Distance: null,
        Laps: 3,
        TrackConfigImage: null,
        BannerImage: null,
        Date: "2026-06-01",
        RegistrationStatus: "Открыта",
        Intro: null,
        Stages: null,
        PersonalStandings: null,
        TeamStandings: null,
        CalendarBannerImage: null,
        OrganizerName: null,
        OrganizerColor: null,
        OrganizerLogo: null);

    private static EventRegistrationRequest RegistrationRequest(string userId) => new(
        userId,
        "Иванов Иван",
        "pilot@example.com",
        "+375 29 123 45 67",
        "Mazda MX-5",
        12.345m,
        "Sport",
        null);

    private sealed class UserRepository(params User[] initial) : IUserRepository
    {
        public List<User> Items { get; } = [.. initial];
        public int UpdateCount { get; private set; }

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<User>>(Items);
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        public Task<User?> FindByEmailOrLoginAsync(string value, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(item => item.Email.Equals(value, StringComparison.OrdinalIgnoreCase) || item.Login.Equals(value, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(item => item.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(item => item.Login.Equals(login, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) => Task.FromResult(Items.Any(item => item.Profile.Phone == phone));
        public Task AddAsync(User user, CancellationToken cancellationToken = default) { Items.Add(user); return Task.CompletedTask; }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) { UpdateCount++; return Task.CompletedTask; }
    }

    private sealed class EventRepository(params RaceEvent[] initial) : IEventRepository
    {
        public List<RaceEvent> Items { get; } = [.. initial];
        public int AddCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int DeleteCount { get; private set; }

        public Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceEvent>>(Items);
        public Task<RaceEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(item => item.Id == id));
        public Task AddAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default) { Items.Add(raceEvent); AddCount++; return Task.CompletedTask; }
        public Task UpdateAsync(RaceEvent raceEvent, CancellationToken cancellationToken = default) { UpdateCount++; return Task.CompletedTask; }
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) { Items.RemoveAll(item => item.Id == id); DeleteCount++; return Task.CompletedTask; }
    }

    private sealed class ResultRepository(params RaceResult[] initial) : IResultRepository
    {
        private readonly List<RaceResult> items = [.. initial];
        public Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceResult>>(items);
        public Task<RaceResult?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(items.FirstOrDefault(item => item.Id == id));
        public Task AddAsync(RaceResult result, CancellationToken cancellationToken = default) { items.Add(result); return Task.CompletedTask; }
        public Task UpdateAsync(RaceResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StartListRepository : IStartListRepository
    {
        public Task<IReadOnlyList<RaceStartListEntry>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceStartListEntry>>([]);
        public Task<IReadOnlyList<RaceStartListEntry>> GenerateAsync(string eventId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceStartListEntry>>([]);
        public Task ClearAsync(string eventId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RaceStartListEntry>> UpdateAsync(string eventId, IReadOnlyList<UpdateStartListEntryRequest> entries, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RaceStartListEntry>>([]);
    }
}
