using GameReleases.Core.DTO;
using GameReleases.Core.Interfaces;

using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IAnalyticsService analyticsService, ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    [HttpGet("top-genres")]
    public async Task<ActionResult<IEnumerable<GenreStatsResponse>>> GetTopGenres([FromQuery] string month)
    {
        var topGenres = await _analyticsService.GetTopGenresAsync(month);
        return Ok(topGenres);
    }

    [HttpGet("dynamics")]
    public async Task<ActionResult<GenreDynamicsResultResponse>> GetDynamics([FromQuery] string months)
    {
        var dynamics = await _analyticsService.GetDynamicsAsync(months);
        return Ok(dynamics);
    }
}