using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Security;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Route("api/results")]
public sealed class ResultsController(JudgeResultService resultService, EventService eventService, EventJudgeService eventJudgeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RaceResult>>> GetAll(
        [FromQuery] string? eventId,
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] string? className,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        IEnumerable<RaceResult> results = await resultService.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(eventId)) results = results.Where(result => result.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(q)) results = results.Where(result => Contains(result.DriverName, q) || Contains(result.CarName, q) || Contains(result.ClassName, q));
        if (!string.IsNullOrWhiteSpace(status)) results = results.Where(result => Contains(result.Status, status));
        if (!string.IsNullOrWhiteSpace(className)) results = results.Where(result => Contains(result.ClassName, className));
        results = (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "points_desc" => results.OrderByDescending(result => result.Points),
            "time" => results.OrderBy(result => result.FinalTimeMs ?? int.MaxValue),
            "driver" => results.OrderBy(result => result.DriverName),
            _ => results.OrderBy(result => result.Position <= 0 ? int.MaxValue : result.Position)
        };
        return Ok(results.ToList());
    }

    [HttpPost]
    [Authorize(Roles = "Судья,Организатор")]
    public async Task<ActionResult<ResultResponse>> Upsert(UpsertRaceResultRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.RequireUserId();
            if (User.IsInRole("Организатор")) await eventService.RequireOrganizerAccessAsync(request.EventId, userId, cancellationToken);
            if (User.IsInRole("Судья") && !DemoAccess.IsGlobalJudge(userId) &&
                !await eventJudgeService.IsAssignedAsync(request.EventId, userId, cancellationToken))
            {
                return Forbid();
            }
            var response = await resultService.UpsertAsync(request with { JudgeUserId = User.IsInRole("Судья") ? userId : null }, cancellationToken);
            return Created($"/api/results/{response.Result.Id}", response);
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/penalties")]
    [Authorize(Roles = "Судья")]
    public async Task<ActionResult<ResultResponse>> AddPenalty(string id, AddPenaltyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await resultService.AddPenaltyAsync(id, request with { JudgeUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/disqualify")]
    [Authorize(Roles = "Судья")]
    public async Task<ActionResult<ResultResponse>> Disqualify(string id, DisqualifyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await resultService.DisqualifyAsync(id, request with { JudgeUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }
    private static bool Contains(string? source, string value) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
