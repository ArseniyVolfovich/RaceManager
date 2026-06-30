using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record CreateChampionshipRequest(
    string Name,
    string Discipline,
    int SeasonYear,
    string? Description,
    string? BannerUrl,
    string? RegulationFileUrl,
    string? Status);

public sealed record UpdateChampionshipRequest(
    string Name,
    string Discipline,
    int SeasonYear,
    string? Description,
    string? BannerUrl,
    string? RegulationFileUrl,
    string? Status);

public sealed record ChampionshipResponse(string Message, Championship Championship);
