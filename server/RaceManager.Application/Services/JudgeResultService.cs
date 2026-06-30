using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Application.Security;
using RaceManager.Domain.Entities;

namespace RaceManager.Application.Services;

public sealed class JudgeResultService(
    IResultRepository results,
    IUserRepository users,
    IEventJudgeRepository eventJudges)
{
    public Task<IReadOnlyList<RaceResult>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return results.GetAllAsync(cancellationToken);
    }

    public async Task<ResultResponse> UpsertAsync(UpsertRaceResultRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.DriverName))
        {
            throw new InvalidOperationException("Укажите этап и пилота.");
        }

        if (!string.IsNullOrWhiteSpace(request.JudgeUserId))
        {
            await RequireAssignedJudgeAsync(request.EventId, request.JudgeUserId, cancellationToken);
        }

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Финишировал" : request.Status.Trim();
        var bestLapMs = BestLap(request.Lap1Ms, request.Lap2Ms, request.Lap3Ms);
        var penaltyMs = Math.Max(0, request.PenaltyMs ?? 0);
        var finalTimeMs = request.FinalTimeMs ?? (bestLapMs is null ? null : bestLapMs.Value + penaltyMs);
        var bestLapText = bestLapMs is null ? request.BestLap?.Trim() ?? string.Empty : FormatMs(bestLapMs.Value);
        var finalTimeText = finalTimeMs is null ? request.LapTime?.Trim() ?? string.Empty : FormatMs(finalTimeMs.Value);
        var result = new RaceResult
        {
            Id = $"result-{Guid.NewGuid():N}"[..15],
            EventId = request.EventId.Trim(),
            StageId = request.StageId?.Trim() ?? string.Empty,
            ParticipantId = request.ParticipantId?.Trim() ?? string.Empty,
            DriverName = request.DriverName.Trim(),
            Position = 0,
            LapTime = finalTimeText,
            BestLap = bestLapText,
            Lap1Ms = request.Lap1Ms,
            Lap2Ms = request.Lap2Ms,
            Lap3Ms = request.Lap3Ms,
            PenaltyMs = penaltyMs,
            FinalTimeMs = finalTimeMs,
            ClassName = request.ClassName?.Trim() ?? string.Empty,
            CarName = request.CarName?.Trim() ?? string.Empty,
            DriverNumber = request.DriverNumber?.Trim() ?? string.Empty,
            Points = 0,
            Status = status
        };

        await results.AddAsync(result, cancellationToken);
        var rankedResult = await RecalculateEventPlacingsAsync(result.EventId, result.Id, cancellationToken);
        return new ResultResponse("Результат сохранен, позиции пересчитаны.", rankedResult);
    }

    public async Task<ResultResponse> AddPenaltyAsync(string resultId, AddPenaltyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await RequireResultAsync(resultId, cancellationToken);
        await RequireAssignedJudgeAsync(result.EventId, request.JudgeUserId, cancellationToken);

        result.Penalties.Add(new RacePenalty
        {
            Id = $"penalty-{Guid.NewGuid():N}"[..16],
            JudgeUserId = request.JudgeUserId,
            Reason = request.Reason.Trim(),
            Points = Math.Max(0, request.Points),
            TimeMs = Math.Max(0, request.TimeMs ?? 0)
        });
        result.PenaltyMs = Math.Max(0, result.PenaltyMs ?? 0) + Math.Max(0, request.TimeMs ?? 0);
        var bestLapMs = BestLap(result.Lap1Ms, result.Lap2Ms, result.Lap3Ms);
        if (bestLapMs is > 0)
        {
            result.FinalTimeMs = bestLapMs.Value + result.PenaltyMs;
            result.LapTime = FormatMs(result.FinalTimeMs.Value);
        }
        result.Points = Math.Max(0, result.Points - Math.Max(0, request.Points));
        result.UpdatedAtUtc = DateTime.UtcNow;

        await results.UpdateAsync(result, cancellationToken);
        var rankedResult = await RecalculateEventPlacingsAsync(result.EventId, result.Id, cancellationToken);
        return new ResultResponse("Штраф добавлен, позиции и очки пересчитаны.", rankedResult);
    }

    public async Task<ResultResponse> DisqualifyAsync(string resultId, DisqualifyRequest request, CancellationToken cancellationToken = default)
    {
        var result = await RequireResultAsync(resultId, cancellationToken);
        await RequireAssignedJudgeAsync(result.EventId, request.JudgeUserId, cancellationToken);

        result.Status = "Дисквалифицирован";
        result.Position = 0;
        result.Points = 0;
        result.Penalties.Add(new RacePenalty
        {
            Id = $"penalty-{Guid.NewGuid():N}"[..16],
            JudgeUserId = request.JudgeUserId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Дисквалификация" : request.Reason.Trim(),
            Points = 0
        });
        result.UpdatedAtUtc = DateTime.UtcNow;

        await results.UpdateAsync(result, cancellationToken);
        var rankedResult = await RecalculateEventPlacingsAsync(result.EventId, result.Id, cancellationToken);
        return new ResultResponse("Участник дисквалифицирован, позиции пересчитаны.", rankedResult);
    }

    public static int CalculatePositionPoints(int position)
    {
        return position switch
        {
            1 => 25,
            2 => 18,
            3 => 15,
            4 => 12,
            5 => 10,
            6 => 8,
            7 => 6,
            8 => 4,
            9 => 2,
            10 => 1,
            _ => 0
        };
    }

    private static int? BestLap(params int?[] laps)
    {
        var valid = laps.Where(value => value is > 0).Select(value => value!.Value).ToArray();
        return valid.Length == 0 ? null : valid.Min();
    }

    private static string FormatMs(int ms) =>
        (ms / 1000m).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

    private async Task<RaceResult> RecalculateEventPlacingsAsync(
        string eventId,
        string requestedResultId,
        CancellationToken cancellationToken)
    {
        var eventResults = (await results.GetAllAsync(cancellationToken))
            .Where(item => string.Equals(item.EventId, eventId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var ranked = eventResults
            .Where(IsRankable)
            .OrderBy(item => ResultTimeMs(item))
            .ThenBy(item => item.BestLap)
            .ThenBy(item => item.DriverName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var positions = ranked
            .Select((item, index) => new { item.Id, Position = index + 1 })
            .ToDictionary(item => item.Id, item => item.Position, StringComparer.OrdinalIgnoreCase);

        foreach (var item in eventResults)
        {
            var position = positions.GetValueOrDefault(item.Id);
            var penaltyPoints = item.Penalties.Sum(penalty => Math.Max(0, penalty.Points));
            var points = position > 0 ? Math.Max(0, CalculatePositionPoints(position) - penaltyPoints) : 0;
            if (item.Position == position && item.Points == points) continue;
            item.Position = position;
            item.Points = points;
            item.UpdatedAtUtc = DateTime.UtcNow;
            await results.UpdateAsync(item, cancellationToken);
        }

        return eventResults.First(item => string.Equals(item.Id, requestedResultId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRankable(RaceResult result)
    {
        var status = result.Status.Trim();
        return !status.Equals("DNF", StringComparison.OrdinalIgnoreCase)
            && !status.Equals("DSQ", StringComparison.OrdinalIgnoreCase)
            && !status.Contains("дисквали", StringComparison.OrdinalIgnoreCase)
            && !status.Contains("ожида", StringComparison.OrdinalIgnoreCase)
            && ResultTimeMs(result) is > 0;
    }

    private static int? ResultTimeMs(RaceResult result)
    {
        if (result.FinalTimeMs is > 0) return result.FinalTimeMs;
        return decimal.TryParse(
            result.LapTime.Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds)
            ? (int)Math.Round(seconds * 1000)
            : null;
    }

    private async Task<RaceResult> RequireResultAsync(string resultId, CancellationToken cancellationToken)
    {
        return await results.GetByIdAsync(resultId, cancellationToken) ?? throw new InvalidOperationException("Результат не найден.");
    }

    private async Task RequireAssignedJudgeAsync(string eventId, string judgeUserId, CancellationToken cancellationToken)
    {
        var judge = await users.GetByIdAsync(judgeUserId, cancellationToken);
        if (judge is null || judge.Role != "Судья")
        {
            throw new InvalidOperationException("Изменять результаты может только судья.");
        }

        if (!DemoAccess.IsGlobalJudge(judgeUserId) &&
            !await eventJudges.IsAssignedAsync(eventId, judgeUserId, cancellationToken))
        {
            throw new InvalidOperationException("Судья не назначен на это событие.");
        }
    }
}
