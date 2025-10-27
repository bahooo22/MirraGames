using GameReleases.Core.DTO;
using GameReleases.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
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
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GenreStatsResponse>>> GetTopGenres([FromQuery] string month)
    {
        _logger.LogInformation("Getting top genres for month: {Month}", month);

        if (string.IsNullOrWhiteSpace(month))
        {
            // Если месяц не указан, используем текущий месяц
            month = DateTime.UtcNow.ToString("yyyy-MM");
            _logger.LogInformation("No month specified, using current month: {Month}", month);
        }

        var topGenres = await _analyticsService.GetTopGenresAsync(month);
        return Ok(topGenres);
    }

    [HttpGet("dynamics")]
    [AllowAnonymous]
    public async Task<ActionResult<GenreDynamicsResultResponse>> GetDynamics([FromQuery] string? months = null)
    {
        _logger.LogInformation("Getting genre dynamics for months: {Months}", months ?? "last 3 months");

        GenreDynamicsResultResponse dynamics;

        if (string.IsNullOrWhiteSpace(months))
        {
            // Если месяцы не указаны, используем последние 3 месяца как в ТЗ
            dynamics = await _analyticsService.GetLastThreeMonthsDynamicsAsync();
        }
        else
        {
            dynamics = await _analyticsService.GetDynamicsAsync(months);
        }

        return Ok(dynamics);
    }

    // 🔧 НОВЫЙ ЭНДПОИНТ: Специально для последних 3 месяцев (явно по ТЗ)
    [HttpGet("dynamics/last-three-months")]
    [AllowAnonymous]
    public async Task<ActionResult<GenreDynamicsResultResponse>> GetLastThreeMonthsDynamics()
    {
        _logger.LogInformation("Getting dynamics for last 3 months explicitly");
        var dynamics = await _analyticsService.GetLastThreeMonthsDynamicsAsync();
        return Ok(dynamics);
    }
}