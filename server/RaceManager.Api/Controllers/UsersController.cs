using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Interfaces;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserRepository users) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Технический администратор")]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await users.GetAllAsync(cancellationToken);
        return Ok(result.Select(user => user.ToDto()).ToList());
    }

    [HttpGet("judges")]
    [Authorize(Roles = "Организатор")]
    public async Task<IActionResult> GetJudges(CancellationToken cancellationToken)
    {
        var result = await users.GetAllAsync(cancellationToken);
        return Ok(result
            .Where(user => user.Role == "Судья")
            .Select(user => new
            {
                user.Id,
                user.Login,
                user.Email,
                FullName = string.Join(" ", new[] { user.Profile.LastName, user.Profile.FirstName, user.Profile.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)))
            })
            .ToList());
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? role, [FromQuery] string? sort, CancellationToken cancellationToken)
    {
        IEnumerable<RaceManager.Domain.Entities.User> result = await users.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(role)) result = result.Where(user => Contains(user.Role, role));
        if (!string.IsNullOrWhiteSpace(q))
        {
            result = result.Where(user => Contains(user.Login, q) || Contains(user.Email, q) || Contains(user.Profile.LastName, q) || Contains(user.Profile.FirstName, q) || Contains(user.Profile.Phone, q));
        }
        result = (sort ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "email" => result.OrderBy(user => user.Email),
            "role" => result.OrderBy(user => user.Role).ThenBy(user => user.Login),
            _ => result.OrderBy(user => user.Login)
        };
        return Ok(result.Select(user => user.ToDto()).ToList());
    }

    [AllowAnonymous]
    [HttpGet("team-catalog")]
    public async Task<IActionResult> GetTeamCatalog([FromQuery] string? q, CancellationToken cancellationToken)
    {
        IEnumerable<RaceManager.Domain.Entities.User> result = await users.GetAllAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(q))
        {
            result = result.Where(user => Contains(user.Profile.OrganizationName, q) || Contains(user.Profile.RacingTeamName, q));
        }
        return Ok(result.Select(user => new
        {
            user.Id,
            profile = new
            {
                user.Profile.OrganizationName,
                user.Profile.OrganizationColor,
                user.Profile.OrganizationLogo,
                user.Profile.OrganizationBanner,
                organizationMembers = PublicMembers(user.Profile.OrganizationMembers),
                user.Profile.RacingTeamName,
                user.Profile.RacingTeamColor,
                user.Profile.RacingTeamLogo,
                user.Profile.RacingTeamBanner,
                racingTeamMembers = PublicMembers(user.Profile.RacingTeamMembers)
            }
        }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(id)) return Forbid();
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound(new { message = "Пользователь не найден." }) : Ok(user.ToDto());
    }

    private static object[] PublicMembers(IEnumerable<RaceManager.Domain.Entities.OrganizationMember> members) =>
        members
            .Where(member => member.Status is "Owner" or "Accepted")
            .Select(member => (object)new { member.FullName, member.Status })
            .ToArray();
    private static bool Contains(string? source, string value) =>
        !string.IsNullOrWhiteSpace(source) && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
