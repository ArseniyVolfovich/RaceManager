using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record UpsertRaceResultRequest(
    string EventId,
    string? StageId,
    string? ParticipantId,
    string DriverName,
    int Position,
    string? LapTime,
    string? BestLap,
    int? Points,
    string? Status,
    string? JudgeUserId,
    int? Lap1Ms = null,
    int? Lap2Ms = null,
    int? Lap3Ms = null,
    int? PenaltyMs = null,
    int? FinalTimeMs = null,
    string? ClassName = null,
    string? CarName = null,
    string? DriverNumber = null);

public sealed record AddPenaltyRequest(string JudgeUserId, string Reason, int Points, int? TimeMs = null);

public sealed record DisqualifyRequest(string JudgeUserId, string Reason);

public sealed record ResultResponse(string Message, RaceResult Result);
