using GameReleases.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameReleases.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SyncController : ControllerBase
{
    private readonly ISteamSyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISteamSyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    /// <summary>
    /// Ручной запуск синхронизации с Steam
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RunSync(CancellationToken cancellationToken)
    {
        try
        {
            await _syncService.SyncAsync(cancellationToken);
            return Ok(new { message = "Sync started successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual sync failed");
            return StatusCode(500, "Error during manual sync");
        }
    }
}