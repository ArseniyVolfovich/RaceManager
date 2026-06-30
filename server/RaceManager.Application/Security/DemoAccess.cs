namespace RaceManager.Application.Security;

public static class DemoAccess
{
    public const string GlobalOrganizerUserId = "organizer-global";
    public const string GlobalJudgeUserId = "judge-global";

    public static bool IsGlobalOrganizer(string? userId) =>
        string.Equals(userId, GlobalOrganizerUserId, StringComparison.OrdinalIgnoreCase);

    public static bool IsGlobalJudge(string? userId) =>
        string.Equals(userId, GlobalJudgeUserId, StringComparison.OrdinalIgnoreCase);
}
