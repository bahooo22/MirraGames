using GameReleases.Core;
using GameReleases.Core.DTO;
using GameReleases.Core.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameReleases.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly ILogger<GamesController> _logger;

    public GamesController(IGameService gameService, ILogger<GamesController> logger)
    {
        _gameService = gameService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<GameResponse>>> GetGames(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? genre = null,
        [FromQuery] string? platform = null)
    {
        try
        {
            var result = await _gameService.GetPagedWithFiltersAsync(page, pageSize, search, genre, platform);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged games");
            return StatusCode(500, "An error occurred while retrieving games");
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> GetGame(Guid id)
    {
        try
        {
            var game = await _gameService.GetByIdAsync(id);
            return game != null ? Ok(game) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the game");
        }
    }

    [HttpGet("app/{appId}")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> GetGameByAppId(string appId)
    {
        try
        {
            var game = await _gameService.GetByAppIdAsync(appId);
            return game != null ? Ok(game) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by AppId: {AppId}", appId);
            return StatusCode(500, "An error occurred while retrieving the game");
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<GameResponse>> CreateGame(CreateGameRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdGame = await _gameService.CreateAsync(request);
            return CreatedAtAction(nameof(GetGame), new { id = createdGame.Id }, createdGame);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game");
            return StatusCode(500, "An error occurred while creating the game");
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<GameResponse>> UpdateGame(Guid id, UpdateGameRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedGame = await _gameService.UpdateAsync(id, request);
            return updatedGame != null ? Ok(updatedGame) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating game with ID: {Id}", id);
            return StatusCode(500, "An error occurred while updating the game");
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteGame(Guid id)
    {
        try
        {
            var result = await _gameService.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting game with ID: {Id}", id);
            return StatusCode(500, "An error occurred while deleting the game");
        }
    }

    /// <summary>
    /// Список релизов за месяц с фильтрацией по платформе и жанру
    /// </summary>
    /// <param name="month"></param>
    /// <param name="platform"></param>
    /// <param name="genre"></param>
    /// <returns></returns>
    [HttpGet("releases")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetReleases(
        [FromQuery] string month,
        [FromQuery] string? platform = null,
        [FromQuery] string? genre = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(month))
                return BadRequest("month is required in format yyyy-MM");

            var releases = await _gameService.GetReleasesAsync(month, platform, genre);
            return Ok(releases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting releases for month {Month}", month);
            return StatusCode(500, "An error occurred while retrieving releases");
        }
    }

    /// <summary>
    /// Календарь релизов: количество игр по дням в указанном месяце
    /// </summary>
    /// <param name="month"></param>
    /// <returns></returns>
    [HttpGet("calendar")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetCalendar([FromQuery] string month)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(month))
                return BadRequest("month is required in format yyyy-MM");

            var calendar = await _gameService.GetCalendarAsync(month);
            return Ok(calendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar for month {Month}", month);
            return StatusCode(500, "An error occurred while retrieving calendar");
        }
    }

    /// <summary>
    /// Псевдоним для соответствия ТЗ: GET /api/v1/games?month=yyyy-MM
    /// </summary>
    /// <param name="month"></param>
    /// <param name="platform"></param>
    /// <param name="genre"></param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GameResponse>>> GetReleasesAlias(
        [FromQuery] string month,
        [FromQuery] string? platform = null,
        [FromQuery] string? genre = null)
    {
        return await GetReleases(month, platform, genre);
    }
}