using RaceManager.Domain.Entities;
using RaceManager.Application.Services;

namespace RaceManager.Application.DTOs;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(
    string Login,
    string Email,
    string Password,
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Phone,
    string? BirthDate,
    string? Car);

public sealed record AuthResponse(string Message, UserDto User);

public sealed record UserDto(
    string Id,
    string Login,
    string Email,
    string Role,
    UserProfile Profile,
    UserStatistics Statistics,
    IReadOnlyList<ChampionshipParticipation> Championships,
    TeamInfo? Team,
    IReadOnlyList<Vehicle> Vehicles,
    IReadOnlyList<UserApplication> Applications,
    string Avatar);

public static class UserDtoMapper
{
    public static UserDto ToDto(this User user)
    {
        TeamComposition.Normalize(user);
        return new UserDto(
            user.Id,
            user.Login,
            user.Email,
            user.Role,
            user.Profile,
            user.Statistics,
            user.Championships,
            user.Team,
            user.Vehicles,
            user.Applications,
            user.Avatar);
    }
}
