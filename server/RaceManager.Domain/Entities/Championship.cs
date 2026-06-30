namespace RaceManager.Domain.Entities;

public sealed class Championship
{
    public string Id { get; set; } = string.Empty;
    public string OrganizerUserId { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SeasonYear { get; set; }
    public string BannerUrl { get; set; } = string.Empty;
    public string RegulationFileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public List<RaceEvent> Events { get; set; } = [];
    public List<StandingRow> DriverStandings { get; set; } = [];
    public List<StandingRow> TeamStandings { get; set; } = [];
}
