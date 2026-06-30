using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Security;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Route("api/events")]
public sealed class EventsController(EventService eventService, StartListService startListService, EventJudgeService eventJudgeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RaceEvent>>> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? discipline,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        CancellationToken cancellationToken)
    {
        IEnumerable<RaceEvent> result = await eventService.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(q))
        {
            result = result.Where(item => Contains(item.Title, q) || Contains(item.Track, q) || Contains(item.OrganizerName, q) || Contains(item.Discipline, q));
        }
        if (!string.IsNullOrWhiteSpace(discipline)) result = result.Where(item => Contains(item.Discipline, discipline));
        if (!string.IsNullOrWhiteSpace(type)) result = result.Where(item => Contains(item.Type, type));
        if (!string.IsNullOrWhiteSpace(status)) result = result.Where(item => Contains(item.RegistrationStatus, status));
        result = (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "title" or "name" => result.OrderBy(item => item.Title),
            "discipline" => result.OrderBy(item => item.Discipline).ThenBy(item => item.Date),
            "track" => result.OrderBy(item => item.Track).ThenBy(item => item.Date),
            "date_desc" => result.OrderByDescending(item => item.Date),
            _ => result.OrderBy(item => item.Date)
        };
        return Ok(result.ToList());
    }

    [HttpGet("judge-assigned")]
    [Authorize(Roles = "Судья")]
    public async Task<IActionResult> GetJudgeAssigned(CancellationToken cancellationToken)
    {
        var events = await eventService.GetAllAsync(cancellationToken);
        if (DemoAccess.IsGlobalJudge(User.RequireUserId()))
        {
            return Ok(events.Select(item => new
            {
                item.Id,
                Name = item.Title,
                item.Type,
                Date = item.Date,
                item.Track,
                item.Discipline,
                Participants = item.Participants
            }).ToList());
        }

        var assignments = await eventJudgeService.GetByJudgeAsync(User.RequireUserId(), cancellationToken);
        var assignedIds = assignments
            .Select(item => item.EventId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var assigned = events
            .Where(item => assignedIds.Contains(item.Id))
            .Select(item => new
            {
                item.Id,
                Name = item.Title,
                item.Type,
                Date = item.Date,
                item.Track,
                item.Discipline,
                Participants = item.Participants
            })
            .ToList();

        return Ok(assigned);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RaceEvent>> GetById(string id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.GetByIdAsync(id, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return NotFound(new { message = error.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Организатор")]
    public async Task<ActionResult<EventResponse>> Create(CreateRaceEventRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await eventService.CreateAsync(request with { OrganizerUserId = User.RequireUserId() }, cancellationToken);
            return Created($"/api/events/{response.Event.Id}", response);
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
        catch (DbUpdateException error)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Не удалось сохранить событие в базе данных. Проверьте структуру таблицы Events.",
                detail = error.InnerException?.Message
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Организатор")]
    public async Task<ActionResult<EventResponse>> Update(string id, UpdateRaceEventRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.UpdateAsync(id, request with { OrganizerUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Организатор")]
    public async Task<ActionResult<EventResponse>> Delete(string id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.DeleteAsync(id, User.RequireUserId(), cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/registrations")]
    [Authorize]
    public async Task<ActionResult<EventResponse>> RegisterParticipant(string id, EventRegistrationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.RegisterParticipantAsync(id, request with { UserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/registrations/{participantId}/reject")]
    [Authorize(Roles = "Организатор")]
    public async Task<ActionResult<EventResponse>> RejectRegistration(string id, string participantId, RejectEventRegistrationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.RejectRegistrationAsync(id, participantId, request with { OrganizerUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/registrations/{participantId}/cancel")]
    [Authorize]
    public async Task<ActionResult<EventResponse>> CancelRegistration(string id, string participantId, CancelEventRegistrationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await eventService.CancelRegistrationAsync(id, participantId, request with { UserId = User.RequireUserId(), Email = null }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpGet("{id}/start-list")]
    public async Task<IActionResult> GetStartList(string id, CancellationToken cancellationToken) =>
        Ok(await startListService.GetAsync(id, cancellationToken));

    [HttpPost("{id}/start-list/generate")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> GenerateStartList(string id, CancellationToken cancellationToken)
    {
        try
        {
            await eventService.RequireOrganizerAccessAsync(id, User.RequireUserId(), cancellationToken);
            return Ok(await startListService.GenerateAsync(id, cancellationToken));
        }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpDelete("{id}/start-list")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> ClearStartList(string id, CancellationToken cancellationToken)
    {
        try
        {
            await eventService.RequireOrganizerAccessAsync(id, User.RequireUserId(), cancellationToken);
            await startListService.ClearAsync(id, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpPut("{id}/start-list")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> UpdateStartList(string id, UpdateStartListRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await eventService.RequireOrganizerAccessAsync(id, User.RequireUserId(), cancellationToken);
            return Ok(await startListService.UpdateAsync(id, request, cancellationToken));
        }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpGet("{id}/judges")]
    public async Task<IActionResult> GetJudges(string id, CancellationToken cancellationToken) =>
        Ok(await eventJudgeService.GetByEventAsync(id, cancellationToken));

    [HttpPost("{id}/judges")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> InviteJudge(string id, InviteEventJudgeRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await eventJudgeService.InviteAsync(id, User.RequireUserId(), request, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpDelete("{id}/judges/{judgeUserId}")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> RemoveJudge(string id, string judgeUserId, CancellationToken cancellationToken)
    {
        try { return Ok(await eventJudgeService.RemoveAsync(id, judgeUserId, User.RequireUserId(), cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }
    private static bool Contains(string? source, string value) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);

}
