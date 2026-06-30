namespace RaceManager.Domain.Entities;

public sealed class RaceStartListEntry
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int RegistrationId { get; set; }
    public int StartNumber { get; set; }
    public int StartPosition { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string DriverNumber { get; set; } = string.Empty;
    public string CarName { get; set; } = string.Empty;
    public string TeamName { get; set; } = "Нету";
    public string ClassName { get; set; } = string.Empty;
}
