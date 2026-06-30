using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;

namespace RaceManager.Tests;

public sealed class InvitationAndSupportTests
{
    [Fact]
    public async Task InviteJudgeAsync_ByEmail_CreatesPendingInvitationWithoutAssignment()
    {
        var organizer = Organizer();
        var judge = Judge();
        var users = new UserRepository([organizer, judge]);
        var assignments = new EventJudgeRepository();
        var events = new EventRepository(Event());
        var service = new EventJudgeService(assignments, events, users);

        var response = await service.InviteAsync(EventId, organizer.Id, new InviteEventJudgeRequest(judge.Email));

        Assert.Equal(judge.Id, response.JudgeUserId);
        var invitation = Assert.Single(judge.Profile.TeamInvitations);
        Assert.Equal("Judge", invitation.TeamType);
        Assert.Equal(EventId, invitation.TargetId);
        Assert.Equal("Pending", invitation.Status);
        Assert.False(await assignments.IsAssignedAsync(EventId, judge.Id));
    }

    [Fact]
    public async Task SendTeamInvitationsAsync_RacingTeam_FindsExactEmailEvenWhenNameDiffers()
    {
        var owner = Organizer();
        owner.Profile.RacingTeamName = "Apex Racing";
        var driver = new User
        {
            Id = "driver-1",
            Login = "driver",
            Email = "driver@example.com",
            Profile = new UserProfile
            {
                FirstName = "Иван",
                LastName = "Петров",
                Phone = "+375 29 765 43 21"
            }
        };
        var service = new UserProfileService(new UserRepository([owner, driver]), new EventJudgeRepository());

        var response = await service.SendTeamInvitationsAsync(
            owner.Id,
            new SendTeamInvitationsRequest("Racing",
            [
                new TeamInviteeRequest("Другое Имя", "", "driver@example.com")
            ]));

        Assert.Empty(response.MissingUsers);
        var invitation = Assert.Single(driver.Profile.TeamInvitations);
        Assert.Equal("Racing", invitation.TeamType);
        Assert.Equal("Apex Racing", invitation.TeamName);
        Assert.Equal("Pending", invitation.Status);
        Assert.False(invitation.IsRead);
    }

    [Fact]
    public async Task RespondTeamInvitationAsync_Accept_AssignsJudgeAndNotifiesOrganizer()
    {
        var organizer = Organizer();
        var judge = JudgeWithInvitation(organizer.Id);
        var users = new UserRepository([organizer, judge]);
        var assignments = new EventJudgeRepository();
        var service = new UserProfileService(users, assignments);

        await service.RespondTeamInvitationAsync(judge.Id, InvitationId, new RespondTeamInvitationRequest(true));

        Assert.True(await assignments.IsAssignedAsync(EventId, judge.Id));
        Assert.Equal("Accepted", judge.Profile.TeamInvitations[0].Status);
        Assert.Contains(organizer.Profile.TeamNotifications, item => item.Message.Contains("принял приглашение", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RespondTeamInvitationAsync_Decline_DoesNotAssignJudgeAndNotifiesOrganizer()
    {
        var organizer = Organizer();
        var judge = JudgeWithInvitation(organizer.Id);
        var users = new UserRepository([organizer, judge]);
        var assignments = new EventJudgeRepository();
        var service = new UserProfileService(users, assignments);

        await service.RespondTeamInvitationAsync(judge.Id, InvitationId, new RespondTeamInvitationRequest(false));

        Assert.False(await assignments.IsAssignedAsync(EventId, judge.Id));
        Assert.Equal("Declined", judge.Profile.TeamInvitations[0].Status);
        Assert.Contains(organizer.Profile.TeamNotifications, item => item.Message.Contains("отклонил приглашение", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Технический администратор")]
    [InlineData("Технический админ")]
    public async Task AnswerAsync_AcceptsBothTechnicalAdminRoleNames(string role)
    {
        var admin = new User { Id = "admin-1", Role = role };
        var ticket = new SupportTicket { Id = "ticket-1", Name = "Пользователь", Email = "user@example.com", Subject = "Вопрос", Message = "Текст" };
        var tickets = new SupportRepository(ticket);
        var service = new SupportService(tickets, new UserRepository([admin]), new EmailSender());

        var response = await service.AnswerAsync(ticket.Id, new AnswerSupportTicketRequest(admin.Id, "Ответ"));

        Assert.Equal("Рассмотренное", response.Ticket.Status);
        Assert.Single(response.Ticket.Answers);
    }

    [Fact]
    public async Task MarkNotificationReadAsync_MarksOnlyRequestedNotification()
    {
        var user = new User { Id = "user-1" };
        user.Profile.TeamNotifications.AddRange(
        [
            new TeamNotification { Id = "notification-1", Message = "Первое" },
            new TeamNotification { Id = "notification-2", Message = "Второе" }
        ]);
        var service = new UserProfileService(new UserRepository([user]), new EventJudgeRepository());

        await service.MarkNotificationReadAsync(user.Id, "notification-1");

        Assert.True(user.Profile.TeamNotifications[0].IsRead);
        Assert.False(user.Profile.TeamNotifications[1].IsRead);
    }

    private const string EventId = "event-1";
    private const string InvitationId = "judge-invite-1";

    private static User Organizer() => new()
    {
        Id = "organizer-1",
        Login = "organizer",
        Role = "Организатор",
        Profile = new UserProfile { FirstName = "Олег", LastName = "Организатор" }
    };

    private static User Judge() => new()
    {
        Id = "judge-1",
        Login = "judge",
        Email = "judge@example.com",
        Role = "Судья",
        Profile = new UserProfile { FirstName = "Иван", LastName = "Судья", Phone = "+375 29 123 45 67" }
    };

    private static User JudgeWithInvitation(string organizerId)
    {
        var judge = Judge();
        judge.Profile.TeamInvitations.Add(new TeamInvitation
        {
            Id = InvitationId,
            OwnerUserId = organizerId,
            OwnerName = "Олег Организатор",
            TeamType = "Judge",
            TargetId = EventId,
            TeamName = "Первый этап",
            Status = "Pending"
        });
        return judge;
    }

    private static RaceEvent Event() => new()
    {
        Id = EventId,
        Title = "Первый этап",
        OrganizerUserId = "organizer-1"
    };

    private sealed class EventRepository(RaceEvent raceEvent) : IEventRepository
    {
        public Task<IReadOnlyList<RaceEvent>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RaceEvent>>([raceEvent]);
        public Task<RaceEvent?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RaceEvent?>(id == raceEvent.Id ? raceEvent : null);
        public Task AddAsync(RaceEvent value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(RaceEvent value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EventJudgeRepository : IEventJudgeRepository
    {
        private readonly HashSet<(string EventId, string JudgeId)> assignments = [];

        public Task<IReadOnlyList<EventJudgeInfo>> GetByEventAsync(string eventId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventJudgeInfo>>([]);
        public Task<IReadOnlyList<EventJudgeInfo>> GetByJudgeAsync(string judgeUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventJudgeInfo>>([]);
        public Task AssignAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default)
        {
            assignments.Add((eventId, judgeUserId));
            return Task.CompletedTask;
        }
        public Task RemoveAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default)
        {
            assignments.Remove((eventId, judgeUserId));
            return Task.CompletedTask;
        }
        public Task<bool> IsAssignedAsync(string eventId, string judgeUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(assignments.Contains((eventId, judgeUserId)));
    }

    private sealed class UserRepository(IEnumerable<User> values) : IUserRepository
    {
        private readonly List<User> users = values.ToList();

        public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>(users);
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.FirstOrDefault(user => user.Id == id));
        public Task<User?> FindByEmailOrLoginAsync(string value, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.FirstOrDefault(user => user.Email == value || user.Login == value));
        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.Any(user => user.Email == email));
        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.Any(user => user.Login == login));
        public Task<bool> PhoneExistsAsync(string phone, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.Any(user => user.Profile.Phone == phone));
        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            users.Add(user);
            return Task.CompletedTask;
        }
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SupportRepository(SupportTicket ticket) : ISupportTicketRepository
    {
        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SupportTicket>>([ticket]);
        public Task<SupportTicket?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<SupportTicket?>(id == ticket.Id ? ticket : null);
        public Task AddAsync(SupportTicket value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(SupportTicket value, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class EmailSender : IEmailSender
    {
        public Task SendHtmlAsync(string to, string subject, string html, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
