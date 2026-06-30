using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceManager.Infrastructure.Database;

namespace RaceManager.Api.Controllers;

[ApiController]
[Route("api/database")]
public sealed class DatabaseController(RaceManagerDbContext dbContext, JsonToSqlImportService importService) : ControllerBase
{
    [HttpGet("ping")]
    public async Task<IActionResult> Ping(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "unavailable",
                database = dbContext.Database.GetDbConnection().Database
            });
        }

        var rolesCount = await dbContext.Roles.CountAsync(cancellationToken);
        var disciplinesCount = await dbContext.Disciplines.CountAsync(cancellationToken);

        return Ok(new
        {
            status = "ok",
            provider = dbContext.Database.ProviderName,
            database = dbContext.Database.GetDbConnection().Database,
            roles = rolesCount,
            disciplines = disciplinesCount
        });
    }

    [HttpPost("import-json")]
    [Authorize(Roles = "Технический администратор")]
    public async Task<IActionResult> ImportJson(CancellationToken cancellationToken)
    {
        var result = await importService.ImportAsync(cancellationToken);
        return Ok(new
        {
            status = "imported",
            result.Users,
            result.Drivers,
            result.Cars,
            result.Championships,
            result.ChampionshipStandings,
            result.SupportTickets,
            result.SupportMessages
        });
    }

}
