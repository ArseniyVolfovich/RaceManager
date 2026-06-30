namespace RaceManager.Infrastructure.JsonStorage;

public sealed class JsonStorageOptions
{
    public string UsersPath { get; set; } = string.Empty;
    public string EventsPath { get; set; } = string.Empty;
    public string ResultsPath { get; set; } = string.Empty;
    public string SupportTicketsPath { get; set; } = string.Empty;
}
