using RaceManager.Domain.Entities;

namespace RaceManager.Application.DTOs;

public sealed record UpdateStartListEntryRequest(int RegistrationId, int StartNumber, int StartPosition);
public sealed record UpdateStartListRequest(IReadOnlyList<UpdateStartListEntryRequest> Entries);
public sealed record StartListResponse(string Message, IReadOnlyList<RaceStartListEntry> Entries);
