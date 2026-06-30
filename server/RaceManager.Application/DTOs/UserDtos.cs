using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record AddVehicleRequest(string Name, string? Image, Dictionary<string, string?> Specs, bool IsTeamVehicle, string? TeamType);

public sealed record AddUserApplicationRequest(
    string EventName,
    string Date,
    string Location,
    string Discipline,
    string? Status);

public sealed record UpdateProfileRequest(
    string? LastName,
    string? FirstName,
    string? MiddleName,
    string? Email,
    string? Phone,
    string? Avatar,
    string? OrganizationName,
    string? OrganizationColor,
    string? OrganizationLogo,
    string? OrganizationBanner,
    IReadOnlyList<OrganizationMember>? OrganizationMembers,
    string? RacingTeamName,
    string? RacingTeamColor,
    string? RacingTeamLogo,
    string? RacingTeamBanner,
    IReadOnlyList<OrganizationMember>? RacingTeamMembers,
    string? DriverNumber);

public sealed record TeamInviteeRequest(string? FullName, string? Phone, string? Email);
public sealed record SendTeamInvitationsRequest(string TeamType, IReadOnlyList<TeamInviteeRequest> Invitees);
public sealed record RespondTeamInvitationRequest(bool Accept);
public sealed record TeamInvitationUpdateResponse(string Message, IReadOnlyList<string> MissingUsers, UserDto User);

public sealed record UserUpdateResponse(string Message, UserDto User);
