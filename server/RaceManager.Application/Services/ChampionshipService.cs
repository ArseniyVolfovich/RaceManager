using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class ChampionshipService(IChampionshipRepository championships, IUserRepository users)
{
    public Task<IReadOnlyList<Championship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        championships.GetAllAsync(cancellationToken);

    public async Task<Championship> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await championships.GetByIdAsync(id, cancellationToken) ?? throw new InvalidOperationException("Чемпионат не найден.");

    public async Task<ChampionshipResponse> CreateAsync(
        string organizerUserId,
        CreateChampionshipRequest request,
        CancellationToken cancellationToken = default)
    {
        await RequireOrganizerAsync(organizerUserId, cancellationToken);
        Validate(request.Name, request.Discipline, request.SeasonYear);
        var championship = new Championship
        {
            OrganizerUserId = organizerUserId,
            Name = request.Name.Trim(),
            Discipline = request.Discipline.Trim(),
            SeasonYear = request.SeasonYear,
            Description = request.Description?.Trim() ?? string.Empty,
            BannerUrl = request.BannerUrl?.Trim() ?? string.Empty,
            RegulationFileUrl = request.RegulationFileUrl?.Trim() ?? string.Empty,
            Status = NormalizeStatus(request.Status)
        };
        await championships.AddAsync(championship, cancellationToken);
        return new ChampionshipResponse("Чемпионат создан.", championship);
    }

    public async Task<ChampionshipResponse> UpdateAsync(
        string id,
        string organizerUserId,
        UpdateChampionshipRequest request,
        CancellationToken cancellationToken = default)
    {
        var championship = await GetByIdAsync(id, cancellationToken);
        if (championship.OrganizerUserId != organizerUserId) throw new InvalidOperationException("Можно изменять только свои чемпионаты.");
        Validate(request.Name, request.Discipline, request.SeasonYear);
        championship.Name = request.Name.Trim();
        championship.Discipline = request.Discipline.Trim();
        championship.SeasonYear = request.SeasonYear;
        championship.Description = request.Description?.Trim() ?? string.Empty;
        championship.BannerUrl = request.BannerUrl?.Trim() ?? string.Empty;
        championship.RegulationFileUrl = request.RegulationFileUrl?.Trim() ?? string.Empty;
        championship.Status = NormalizeStatus(request.Status);
        await championships.UpdateAsync(championship, cancellationToken);
        return new ChampionshipResponse("Чемпионат обновлён.", championship);
    }

    public async Task DeleteAsync(string id, string organizerUserId, CancellationToken cancellationToken = default)
    {
        var championship = await GetByIdAsync(id, cancellationToken);
        if (championship.OrganizerUserId != organizerUserId) throw new InvalidOperationException("Можно удалять только свои чемпионаты.");
        if (championship.Events.Count > 0) throw new InvalidOperationException("Сначала удалите этапы чемпионата.");
        await championships.DeleteAsync(id, cancellationToken);
    }

    private async Task RequireOrganizerAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user?.Role != "Организатор") throw new InvalidOperationException("Создавать чемпионаты может только организатор.");
    }

    private static void Validate(string name, string discipline, int year)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Укажите название чемпионата.");
        if (string.IsNullOrWhiteSpace(discipline)) throw new InvalidOperationException("Укажите дисциплину чемпионата.");
        if (year is < 2000 or > 2100) throw new InvalidOperationException("Укажите корректный год сезона.");
    }

    private static string NormalizeStatus(string? status) => status?.Trim() switch
    {
        "Published" or "Active" or "Completed" or "Cancelled" => status.Trim(),
        _ => "Draft"
    };
}
