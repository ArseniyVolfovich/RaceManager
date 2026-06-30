using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class ReportService(
    IEventRepository events,
    IResultRepository results,
    IUserRepository users,
    IChampionshipRepository? championships = null)
{
    public async Task<ReportDocument> BuildEventReportAsync(string eventId, CancellationToken cancellationToken = default)
    {
        var raceEvent = await events.GetByIdAsync(eventId, cancellationToken) ?? throw new InvalidOperationException("Событие не найдено.");
        var eventResults = (await results.GetAllAsync(cancellationToken))
            .Where(result => string.Equals(result.EventId, raceEvent.Id, StringComparison.OrdinalIgnoreCase))
            .OrderBy(result => result.Position <= 0 ? int.MaxValue : result.Position)
            .ThenBy(result => result.FinalTimeMs ?? int.MaxValue)
            .ToList();

        var rows = eventResults.Select(result => new[]
        {
            result.Position > 0 ? result.Position.ToString() : "—",
            result.DriverNumber,
            result.DriverName,
            result.CarName,
            result.ClassName,
            result.BestLap,
            FormatMilliseconds(result.PenaltyMs),
            result.LapTime,
            result.Points.ToString(),
            result.Status
        }).ToList();

        if (rows.Count == 0)
        {
            rows = raceEvent.Participants.Select((participant, index) => new[]
            {
                (index + 1).ToString(),
                participant.DriverNumber,
                participant.FullName,
                participant.Car,
                participant.ClassName,
                "—",
                "—",
                "—",
                "0",
                participant.Status
            }).ToList();
        }

        return new ReportDocument(
            $"Итоги соревнования: {raceEvent.Title}",
            raceEvent.OrganizerName,
            raceEvent.Track,
            DateTime.UtcNow,
            new[] { "Позиция", "№", "Пилот", "Автомобиль", "Класс", "Лучший круг", "Штраф", "Итог", "Очки", "Статус" },
            rows,
            new[]
            {
                $"Дисциплина: {raceEvent.Discipline}",
                $"Дата проведения: {raceEvent.Date}",
                $"Тип: {raceEvent.Type}",
                $"Статус регистрации: {raceEvent.RegistrationStatus}"
            });
    }

    public async Task<ReportDocument> BuildPilotReportAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken) ?? throw new InvalidOperationException("Пилот не найден.");
        var fullName = string.Join(" ", new[] { user.Profile.LastName, user.Profile.FirstName }.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (string.IsNullOrWhiteSpace(fullName)) fullName = user.Login;

        var rows = user.Applications.Select(item => new[] { item.EventName, item.Date, item.Location, item.Discipline, item.Status }).ToList();
        return new ReportDocument(
            $"Статистика пилота: {fullName}",
            "RaceManager",
            "—",
            DateTime.UtcNow,
            new[] { "Событие", "Дата", "Трасса", "Дисциплина", "Статус" },
            rows,
            new[]
            {
                $"Очки: {user.Statistics.Points}",
                $"Гонки: {user.Statistics.Races}",
                $"Победы: {user.Statistics.Wins}",
                $"Подиумы: {user.Statistics.Podiums}"
            });
    }

    public async Task<ReportDocument> BuildTeamReportAsync(string teamName, CancellationToken cancellationToken = default)
    {
        var allUsers = await users.GetAllAsync(cancellationToken);
        var members = allUsers.Where(user =>
            string.Equals(user.Profile.RacingTeamName, teamName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(user.Profile.OrganizationName, teamName, StringComparison.OrdinalIgnoreCase) ||
            user.Profile.TeamMemberships.Any(item => string.Equals(item.TeamName, teamName, StringComparison.OrdinalIgnoreCase))).ToList();

        var rows = members.Select(user => new[]
        {
            string.Join(" ", new[] { user.Profile.LastName, user.Profile.FirstName }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim(),
            user.Email,
            user.Profile.Phone,
            user.Role,
            user.Statistics.Points.ToString()
        }).ToList();

        return new ReportDocument(
            $"Статистика команды: {teamName}",
            "RaceManager",
            "—",
            DateTime.UtcNow,
            new[] { "Участник", "Почта", "Телефон", "Роль", "Очки" },
            rows,
            new[] { $"Участников: {members.Count}" });
    }

public async Task<ReportDocument> BuildChampionshipReportAsync(string championshipIdOrName, CancellationToken cancellationToken = default)
{
    if (championships is not null)
    {
        var championshipEntity = (await championships.GetAllAsync(cancellationToken)).FirstOrDefault(item =>
            string.Equals(item.Id, championshipIdOrName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Name, championshipIdOrName, StringComparison.OrdinalIgnoreCase));
        if (championshipEntity is null) throw new InvalidOperationException("Чемпионат не найден.");
        var championshipRows = championshipEntity.Events.OrderBy(item => item.Date).Select(item => new[]
        {
            item.Title,
            item.Date,
            item.Track,
            item.Discipline,
            item.Participants.Count.ToString(),
            item.RegistrationStatus
        }).ToList();
        return new ReportDocument(
            $"Отчёт по чемпионату: {championshipEntity.Name}",
            championshipEntity.OrganizerUserId,
            championshipRows.FirstOrDefault()?.ElementAtOrDefault(2) ?? "—",
            DateTime.UtcNow,
            new[] { "Этап", "Дата", "Трасса", "Дисциплина", "Участников", "Регистрация" },
            championshipRows,
            new[]
            {
                $"Сезон: {championshipEntity.SeasonYear}",
                $"Этапов: {championshipRows.Count}",
                $"Пилотов в зачёте: {championshipEntity.DriverStandings.Count}",
                $"Команд в зачёте: {championshipEntity.TeamStandings.Count}"
            });
    }

    var championship = (await events.GetAllAsync(cancellationToken)).FirstOrDefault(item =>
        string.Equals(item.Id, championshipIdOrName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(item.Title, championshipIdOrName, StringComparison.OrdinalIgnoreCase));
    if (championship is null || !string.Equals(championship.Type, "Чемпионат", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("Чемпионат не найден.");
    }

    var rows = championship.Stages.Count > 0
        ? championship.Stages.OrderBy(stage => stage.Date).Select(stage => new[]
        {
            stage.Title,
            stage.Date,
            championship.Track,
            championship.Discipline,
            championship.Participants.Count.ToString(),
            stage.RegistrationStatus
        }).ToList()
        : new List<string[]>
        {
            new[]
            {
                championship.Title,
                championship.Date,
                championship.Track,
                championship.Discipline,
                championship.Participants.Count.ToString(),
                championship.RegistrationStatus
            }
        };

    return new ReportDocument(
        $"Отчёт по чемпионату: {championship.Title}",
        string.IsNullOrWhiteSpace(championship.OrganizerName) ? "RaceManager" : championship.OrganizerName,
        championship.Track,
        DateTime.UtcNow,
        new[] { "Этап", "Дата", "Трасса", "Дисциплина", "Участников", "Регистрация" },
        rows,
        new[] { $"Этапов/событий: {rows.Count}" });
}

    private static string FormatMilliseconds(int? value) => value is null ? "—" : (value.Value / 1000m).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record ReportDocument(
    string Title,
    string Organizer,
    string Track,
    DateTime CreatedAtUtc,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<string> Summary);
