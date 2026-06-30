using System.Security.Claims;

namespace RaceManager.Api.Security;

public static class ControllerUserExtensions
{
    public static string RequireUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Серверная сессия не содержит идентификатор пользователя.");

    public static bool CanAccessUser(this ClaimsPrincipal user, string userId) =>
        user.IsInRole("Технический администратор") ||
        string.Equals(user.FindFirstValue(ClaimTypes.NameIdentifier), userId, StringComparison.OrdinalIgnoreCase);
}
