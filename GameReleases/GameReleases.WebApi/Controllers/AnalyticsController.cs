using GameReleases.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace GameReleases.WebApi.Controllers;

[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly ISteamService _steamService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(ISteamService steamService, ILogger<AnalyticsController> logger)
    {
        _steamService = steamService;
        _logger = logger;
    }

    [HttpGet("top-genres")]
    public async Task<ActionResult<IEnumerable<object>>> GetTopGenres()
    {
        try
        {
            var topGenres = await _steamService.GetTopGenresAsync();
            return Ok(topGenres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top genres");
            return StatusCode(500, "An error occurred while retrieving top genres");
        }
    }

    [HttpGet("dynamics")]
    public async Task<ActionResult<object>> GetDynamics()
    {
        try
        {
            var dynamics = await _steamService.GetDynamicsAsync();
            return Ok(dynamics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving genre dynamics");
            return StatusCode(500, "An error occurred while retrieving genre dynamics");
        }
    }
}