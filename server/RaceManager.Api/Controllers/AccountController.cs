using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using RaceManager.Application.DTOs;
using RaceManager.Application.Services;
using RaceManager.Api.Security;

namespace RaceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController(UserProfileService profileService, ILogger<AccountController> logger) : ControllerBase
{
    [HttpPost("{userId}/vehicles")]
    public async Task<ActionResult<UserUpdateResponse>> AddVehicle(string userId, AddVehicleRequest request, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try
        {
            return Ok(await profileService.AddVehicleAsync(userId, request, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
        catch (DbUpdateException error)
        {
            logger.LogError(error, "Не удалось сохранить автомобиль пользователя {UserId}.", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                message = "Не удалось сохранить автомобиль в базе данных."
            });
        }
    }

    [HttpDelete("{userId}/vehicles/{vehicleId}")]
    public async Task<ActionResult<UserUpdateResponse>> DeleteVehicle(string userId, string vehicleId, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try
        {
            return Ok(await profileService.DeleteVehicleAsync(userId, vehicleId, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }

    [HttpPost("{userId}/applications")]
    public async Task<ActionResult<UserUpdateResponse>> AddApplication(string userId, AddUserApplicationRequest request, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try
        {
            return Ok(await profileService.AddApplicationAsync(userId, request, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }


    [HttpPost("{userId}/team-invitations")]
    public async Task<ActionResult<TeamInvitationUpdateResponse>> SendTeamInvitations(string userId, SendTeamInvitationsRequest request, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try { return Ok(await profileService.SendTeamInvitationsAsync(userId, request, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpPost("{userId}/team-invitations/{invitationId}/respond")]
    public async Task<ActionResult<TeamInvitationUpdateResponse>> RespondTeamInvitation(string userId, string invitationId, RespondTeamInvitationRequest request, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try { return Ok(await profileService.RespondTeamInvitationAsync(userId, invitationId, request, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpDelete("{userId}/team-invitations/{targetUserId}")]
    public async Task<ActionResult<UserUpdateResponse>> CancelTeamInvitation(string userId, string targetUserId, [FromQuery] string teamType, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try { return Ok(await profileService.CancelTeamInvitationAsync(userId, targetUserId, teamType, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpPost("{userId}/notifications/read")]
    public async Task<ActionResult<UserUpdateResponse>> MarkNotificationsRead(string userId, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try { return Ok(await profileService.MarkNotificationsReadAsync(userId, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpPost("{userId}/notifications/{notificationId}/read")]
    public async Task<ActionResult<UserUpdateResponse>> MarkNotificationRead(
        string userId,
        string notificationId,
        CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try { return Ok(await profileService.MarkNotificationReadAsync(userId, notificationId, cancellationToken)); }
        catch (InvalidOperationException error) { return BadRequest(new { message = error.Message }); }
    }

    [HttpPut("{userId}/profile")]
    public async Task<ActionResult<UserUpdateResponse>> UpdateProfile(string userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!User.CanAccessUser(userId)) return Forbid();
        try
        {
            return Ok(await profileService.UpdateProfileAsync(userId, request, cancellationToken));
        }
        catch (InvalidOperationException error)
        {
            return BadRequest(new { message = error.Message });
        }
    }
}
