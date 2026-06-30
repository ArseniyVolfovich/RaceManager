using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record CreateRaceEventRequest(
    string OrganizerUserId,
    string Type,
    string Title,
    string Discipline,
    int ParticipantLimit,
    string Track,
    string? Distance,
    int? Laps,
    string? TrackConfigImage,
    string? BannerImage,
    string Date,
    string? RegistrationStatus,
    string? Intro,
    IReadOnlyList<CreateRaceEventStageRequest>? Stages,
    IReadOnlyList<StandingRow>? PersonalStandings,
    IReadOnlyList<StandingRow>? TeamStandings,
    string? CalendarBannerImage,
    string? OrganizerName,
    string? OrganizerColor,
    string? OrganizerLogo,
    string? ChampionshipId = null);

public sealed record CreateRaceEventStageRequest(
    string Title,
    string Date,
    string? Intro,
    string? RegistrationStatus,
    string? BannerImage);

public sealed record UpdateRaceEventRequest(
    string OrganizerUserId,
    string Type,
    string Title,
    string Discipline,
    int ParticipantLimit,
    string Track,
    string? Distance,
    int? Laps,
    string? TrackConfigImage,
    string? BannerImage,
    string Date,
    string? RegistrationStatus,
    string? Intro,
    IReadOnlyList<CreateRaceEventStageRequest>? Stages,
    IReadOnlyList<StandingRow>? PersonalStandings,
    IReadOnlyList<StandingRow>? TeamStandings,
    string? CalendarBannerImage,
    string? OrganizerName,
    string? OrganizerColor,
    string? OrganizerLogo,
    string? ChampionshipId = null);

public sealed record EventRegistrationRequest(
    string? UserId,
    string FullName,
    string Email,
    string Phone,
    string Car,
    decimal? QualificationTimeSeconds,
    string? ClassName,
    string? TeamName);

public sealed record CancelEventRegistrationRequest(string? UserId, string? Email);

public sealed record RejectEventRegistrationRequest(string OrganizerUserId, string? Email);

public sealed record EventResponse(string Message, RaceEvent Event);
