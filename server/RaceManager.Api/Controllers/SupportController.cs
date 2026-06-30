using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Services;
using RaceManager.Domain.Entities;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Route("api/support/tickets")]
public sealed class SupportController(SupportService supportService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Технический администратор,Технический админ")]
    public async Task<ActionResult<IReadOnlyList<SupportTicket>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await supportService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<SupportTicketResponse>> Create(CreateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.RequireUserId() : null;
            var response = await supportService.CreateAsync(request with { UserId = userId }, cancellationToken);
            return Created($"/api/support/tickets/{response.Ticket.Id}", response);
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/answer")]
    [Authorize(Roles = "Технический администратор,Технический админ")]
    public async Task<ActionResult<SupportTicketResponse>> Answer(string id, AnswerSupportTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await supportService.AnswerAsync(id, request with { AdminUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Технический администратор,Технический админ")]
    public async Task<ActionResult<SupportTicketResponse>> Reject(string id, RejectSupportTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await supportService.RejectAsync(id, request with { AdminUserId = User.RequireUserId() }, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }
}
