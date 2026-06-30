namespace RaceManager.Domain.Entities;

public sealed class RaceEvent
{
    public string Id { get; set; } = string.Empty;
    public string ChampionshipId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public int ParticipantLimit { get; set; }
    public string Track { get; set; } = string.Empty;
    public string Distance { get; set; } = string.Empty;
    public int? Laps { get; set; }
    public string TrackConfigImage { get; set; } = string.Empty;
    public string BannerImage { get; set; } = string.Empty;
    public string CalendarBannerImage { get; set; } = string.Empty;
    public string OrganizerName { get; set; } = string.Empty;
    public string OrganizerColor { get; set; } = "#e10600";
    public string OrganizerLogo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = "Открыта";
    public string Intro { get; set; } = string.Empty;
    public string OrganizerUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<RaceEventStage> Stages { get; set; } = [];
    public List<EventParticipant> Participants { get; set; } = [];
    public List<StandingRow> PersonalStandings { get; set; } = [];
    public List<StandingRow> TeamStandings { get; set; } = [];
}

public sealed class RaceEventStage
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Intro { get; set; } = string.Empty;
    public string RegistrationStatus { get; set; } = "Открыта";
    public string BannerImage { get; set; } = string.Empty;
}

public sealed class EventParticipant
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public string TeamName { get; set; } = "Нету";
    public string DriverNumber { get; set; } = string.Empty;
    public decimal? QualificationTimeSeconds { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Status { get; set; } = "Зарегистрирован";
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class StandingRow
{
    public int Position { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public int Points { get; set; }
}

public sealed class EventJudgeInfo
{
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string EventDate { get; set; } = string.Empty;
    public string EventTrack { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
