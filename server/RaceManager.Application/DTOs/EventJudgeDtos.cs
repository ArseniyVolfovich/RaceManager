using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record InviteEventJudgeRequest(string Identifier);

public sealed record EventJudgeInvitationResponse(string Message, string JudgeUserId);

public sealed record EventJudgeResponse(string Message, IReadOnlyList<EventJudgeInfo> Judges);
