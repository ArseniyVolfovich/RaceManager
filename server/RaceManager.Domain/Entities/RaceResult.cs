namespace RaceManager.Domain.Entities;

public sealed class RaceResult
{
    public string Id { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string StageId { get; set; } = string.Empty;
    public string ParticipantId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public int Position { get; set; }
    public string LapTime { get; set; } = string.Empty;
    public string BestLap { get; set; } = string.Empty;
    public int? Lap1Ms { get; set; }
    public int? Lap2Ms { get; set; }
    public int? Lap3Ms { get; set; }
    public int? PenaltyMs { get; set; }
    public int? FinalTimeMs { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string CarName { get; set; } = string.Empty;
    public string DriverNumber { get; set; } = string.Empty;
    public int Points { get; set; }
    public string Status { get; set; } = "Ожидает проверки";
    public List<RacePenalty> Penalties { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class RacePenalty
{
    public string Id { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Points { get; set; }
    public int TimeMs { get; set; }
    public string JudgeUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
