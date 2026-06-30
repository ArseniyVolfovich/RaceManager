using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Api.Security;
using RaceManager.Application.DTOs;
using RaceManager.Application.Services;

namespace RaceManager.Api.Controllers;

[ApiController]
[Route("api/championships")]
public sealed class ChampionshipsController(ChampionshipService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) => Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        try { return Ok(await service.GetByIdAsync(id, cancellationToken)); }
        catch (InvalidOperationException error) { return NotFound(new { message = error.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> Create(CreateChampionshipRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.CreateAsync(User.RequireUserId(), request, cancellationToken);
            return Created($"/api/championships/{response.Championship.Id}", response);
        }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
        catch (DbUpdateException error)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Не удалось сохранить чемпионат в базе данных.",
                detail = error.InnerException?.Message ?? error.Message
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> Update(string id, UpdateChampionshipRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await service.UpdateAsync(id, User.RequireUserId(), request, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        try { await service.DeleteAsync(id, User.RequireUserId(), cancellationToken); return NoContent(); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }
}
