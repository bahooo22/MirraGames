using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

using GameReleases.Core.DTO;
using GameReleases.Core.Interfaces;
using GameReleases.Core.Models;
using GameReleases.Infrastructure.Entities;
using GameReleases.Infrastructure.Interfaces;

using HtmlAgilityPack;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Playwright;

namespace GameReleases.Core.Services;

public abstract class Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    : IServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    where TEntity : class
    where TCreateRequest : class
    where TUpdateRequest : class
    where TResponse : class
    where TId : notnull
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly ILogger _logger;

    protected Services(IRepository<TEntity> repository, ILogger<Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Abstract methods for mapping
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected abstract TEntity MapToEntity(TCreateRequest request);
    protected abstract TResponse MapToResponse(TEntity entity);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

    /// <summary>
    /// Read
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<TResponse?> GetByIdAsync(TId id)
    {
        try
        {
            if (id is Guid guidId)
            {
                var entity = await _repository.GetByIdAsync(guidId);
                return entity != null ? MapToResponse(entity) : null;
            }

            throw new InvalidOperationException($"ID must be of type Guid, but got {id?.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityName} by ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    public virtual async Task<IEnumerable<TResponse>> GetAllAsync()
    {
        try
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityName}", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<IEnumerable<TResponse>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var entities = await _repository.FindAsync(predicate);
            return entities.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding {EntityName}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Pagination
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="predicate"></param>
    /// <param name="orderBy"></param>
    /// <param name="ascending"></param>
    /// <returns></returns>
    public virtual async Task<PagedResponse<TResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true)
    {
        try
        {
            var (items, totalCount) = await _repository.GetPagedAsync(
                pageNumber, pageSize, predicate, orderBy, ascending);

            return new PagedResponse<TResponse>
            {
                Items = items.Select(MapToResponse),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged {EntityName}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Create
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual async Task<TResponse> CreateAsync(TCreateRequest request)
    {
        try
        {
            var entity = MapToEntity(request);
            var createdEntity = await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToResponse(createdEntity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityName}", typeof(TEntity).Name);
            throw;
        }
    }

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual async Task<TResponse?> UpdateAsync(TId id, TUpdateRequest request)
    {
        try
        {
            if (id is Guid guidId)
            {
                var entity = await _repository.GetByIdAsync(guidId);
                if (entity == null)
                {
                    return null;
                }

                UpdateEntity(entity, request);
                await _repository.UpdateAsync(entity);
                await _repository.SaveChangesAsync();

                return MapToResponse(entity);
            }

            throw new InvalidOperationException($"ID must be of type Guid, but got {id?.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityName} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Delete
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<bool> DeleteAsync(TId id)
    {
        try
        {
            if (id is Guid guidId)
            {
                var entity = await _repository.GetByIdAsync(guidId);
                if (entity == null)
                {
                    return false;
                }

                _repository.Remove(entity);
                await _repository.SaveChangesAsync();

                return true;
            }

            throw new InvalidOperationException($"ID must be of type Guid, but got {id?.GetType().Name}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityName} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }
    /// <summary>
    /// Utility
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            return await _repository.ExistsAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if {EntityName} exists", typeof(TEntity).Name);
            throw;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        try
        {
            return await _repository.CountAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting {EntityName}", typeof(TEntity).Name);
            throw;
        }
    }
}

public class SteamFollowersService : ISteamFollowersService, IAsyncDisposable, IDisposable
{
    private readonly ILogger<SteamFollowersService> _logger;
    private readonly SemaphoreSlim _semaphore = new(2, 2);
    private readonly Dictionary<ulong, (DateTime fetched, ulong followers)> _cache = new();

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public SteamFollowersService(ILogger<SteamFollowersService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Playwright...");
        _playwright = await Playwright.CreateAsync();

        _logger.LogInformation("Launching Chromium...");
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-gpu"]
        });

        _logger.LogInformation("Creating browser context...");
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            Locale = "en-US",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        _logger.LogInformation("Opening new page...");
        _page = await _context.NewPageAsync();
    }

    public async Task<ulong> GetFollowersAsync(ulong appId, CancellationToken ct = default)
    {
        if (_page == null)
            await InitializeAsync();

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_cache.TryGetValue(appId, out var cached) && (DateTime.UtcNow - cached.fetched).TotalHours < 6)
            {
                _logger.LogInformation("Using cached value {Followers} for AppId={AppId}", cached.followers, appId);
                return cached.followers;
            }

            var url = $"https://steamcommunity.com/search/groups/?text={appId}";
            _logger.LogInformation("Navigating to {Url}", url);
            await _page!.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            try
            {
                _logger.LogInformation("Waiting for search_row selector...");
                await _page.WaitForSelectorAsync("div.search_row", new PageWaitForSelectorOptions { Timeout = 7000 });

                // точечно берём второй div и сам span с числом
                var membersText = await _page.InnerTextAsync("div.search_row div.searchPersonaInfo div:nth-of-type(2) span");
                _logger.LogInformation("Raw members text for AppId={AppId}: {Text}", appId, membersText);

                var normalized = membersText.Trim().Replace("\u00A0", "").Replace(" ", "");
                _logger.LogInformation("Normalized text for AppId={AppId}: '{Text}'", appId, normalized);

                if (ulong.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var followers))
                {
                    _cache[appId] = (DateTime.UtcNow, followers);
                    _logger.LogInformation("✔ Parsed {Followers} followers for AppId={AppId}", followers, appId);
                    return followers;
                }
                else
                {
                    var digits = Regex.Match(normalized, @"\d+").Value;
                    if (ulong.TryParse(digits, out followers))
                    {
                        _cache[appId] = (DateTime.UtcNow, followers);
                        _logger.LogInformation("✔ Parsed {Followers} followers for AppId={AppId}", followers, appId);
                        return followers;
                    }
                    _logger.LogWarning("Failed to parse followers count from '{MembersText}' for AppId={AppId}", membersText, appId);
                }

            }
            catch (TimeoutException)
            {
                _logger.LogWarning("⚠ Timeout waiting for selector on AppId={AppId}", appId);
            }

            _logger.LogWarning("Returning 0 followers for AppId={AppId}", appId);
            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing Playwright resources...");
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public class GameService(
    IAnalyticsRepository analyticsRepository,
    IGameRepository gameRepository,
    ILogger<GameService> logger)
    : Services<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>(gameRepository, logger), IGameService
{

    /// <summary>
    /// Реализация абстрактных методов маппинга
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    protected override Game MapToEntity(CreateGameRequest request)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            AppId = request.AppId,
            Name = request.Name,
            ReleaseDate = request.ReleaseDate,
            Genres = request.Genres,
            Followers = request.Followers,
            StoreUrl = request.StoreUrl,
            PosterUrl = request.PosterUrl,
            ShortDescription = request.ShortDescription,
            Platforms = request.Platforms,
            CollectedAt = DateTime.UtcNow
        };
    }

    protected override GameResponse MapToResponse(Game entity)
    {
        return new GameResponse
        {
            Id = entity.Id,
            AppId = entity.AppId,
            Name = entity.Name,
            ReleaseDate = entity.ReleaseDate,
            Genres = entity.Genres,
            Followers = entity.Followers,
            StoreUrl = entity.StoreUrl,
            PosterUrl = entity.PosterUrl,
            ShortDescription = entity.ShortDescription,
            Platforms = entity.Platforms,
            CollectedAt = entity.CollectedAt
        };
    }

    protected override void UpdateEntity(Game entity, UpdateGameRequest request)
    {
        entity.Name = request.Name;
        entity.ReleaseDate = request.ReleaseDate;
        entity.Genres = request.Genres;
        entity.Followers = request.Followers;
        entity.StoreUrl = request.StoreUrl;
        entity.PosterUrl = request.PosterUrl;
        entity.ShortDescription = request.ShortDescription;
        entity.Platforms = request.Platforms;
        entity.CollectedAt = DateTime.UtcNow;
    }

    public override async Task<GameResponse?> UpdateAsync(Guid id, UpdateGameRequest request)
    {
        var entity = await gameRepository.GetByIdAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"Game with Id '{id}' not found");

        // Обновляем поля
        UpdateEntity(entity, request);

        await gameRepository.UpdateAsync(entity);

        // Сохраняем снапшот в ClickHouse
        await analyticsRepository.StoreGameAnalyticsAsync(entity);

        return MapToResponse(entity);
    }

    public async Task<IEnumerable<GameResponse>> GetReleasesAsync(string month, string? platform = null, string? genre = null)
    {
        if (!DateTime.TryParse($"{month}-01", out var monthDate))
            throw new ArgumentException("month must be in format yyyy-MM", nameof(month));

        var startDate = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var games = await gameRepository.GetGamesByDateRangeAsync(startDate, endDate);

        if (!string.IsNullOrEmpty(platform))
            games = games.Where(g => g.Platforms.Contains(platform, StringComparer.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(genre))
            games = games.Where(g => g.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase));

        return games.Select(GameResponse.FromEntity);
    }

    public async Task<CalendarResponse> GetCalendarAsync(string month)
    {
        if (!DateTime.TryParse($"{month}-01", out var monthDate))
            throw new ArgumentException("month must be in format yyyy-MM", nameof(month));

        var startDate = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var games = await gameRepository.GetGamesByDateRangeAsync(startDate, endDate);

        var days = games
            .GroupBy(g => g.ReleaseDate?.Date)
            .Where(g => g.Key.HasValue)
            .Select(g => new CalendarDayResponse
            {
                Date = g.Key!.Value,
                Count = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new CalendarResponse
        {
            Month = month,
            Days = days
        };
    }


    /// <summary>
    /// Переопределение CreateAsync для дополнительной валидации
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override async Task<GameResponse> CreateAsync(CreateGameRequest request)
    {
        var existingGame = await gameRepository.GetByAppIdAsync(request.AppId);
        if (existingGame != null)
            throw new InvalidOperationException($"Game with AppId '{request.AppId}' already exists");

        var entity = MapToEntity(request);
        await gameRepository.AddAsync(entity);

        // Сохраняем снапшот в ClickHouse
        await analyticsRepository.StoreGameAnalyticsAsync(entity);

        return MapToResponse(entity);
    }

    /// <summary>
    /// Специфичные методы для Game
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    public async Task<GameResponse?> GetByAppIdAsync(ulong appId)
    {
        try
        {
            var game = await gameRepository.GetByAppIdAsync(appId);
            return game != null ? MapToResponse(game) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting game by AppID: {AppId}", appId);
            throw;
        }
    }

    public async Task<IEnumerable<GameResponse>> GetByGenreAsync(string genre)
    {
        try
        {
            var games = await gameRepository.GetByGenreAsync(genre);
            return games.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting games by genre: {Genre}", genre);
            throw;
        }
    }

    public async Task<IEnumerable<GameResponse>> GetRecentGamesAsync(int days = 7)
    {
        try
        {
            var games = await gameRepository.GetRecentGamesAsync(days);
            return games.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent games for {Days} days", days);
            throw;
        }
    }

    public async Task<IEnumerable<GameResponse>> GetPopularGamesAsync(int count = 10)
    {
        try
        {
            var games = await gameRepository.GetPopularGamesAsync(count);
            return games.Select(MapToResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {Count} popular games", count);
            throw;
        }
    }

    public async Task<PagedResponse<GameResponse>> GetReleasesPagedAsync(
        string month,
        string? platform = null,
        string? genre = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        // Логика получения релизов по месяцу + пагинация
        var games = await GetReleasesAsync(month, platform, genre);
        var pagedGames = games.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        return new PagedResponse<GameResponse>
        {
            Items = pagedGames,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = games.Count()
        };
    }

    public async Task<PagedResponse<GameResponse>> GetPagedWithFiltersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? genre = null,
        string? platform = null)
    {
        try
        {
            Expression<Func<Game, bool>>? predicate = null;

            if (!string.IsNullOrWhiteSpace(searchTerm) || !string.IsNullOrWhiteSpace(genre) || !string.IsNullOrWhiteSpace(platform))
            {
                predicate = game =>
                    (string.IsNullOrWhiteSpace(searchTerm) ||
                     game.Name.Contains(searchTerm) ||
                     game.ShortDescription.Contains(searchTerm)) &&
                    (string.IsNullOrWhiteSpace(genre) ||
                     game.Genres.Any(g => g.Contains(genre))) &&
                    (string.IsNullOrWhiteSpace(platform) ||
                     game.Platforms.Any(p => p.Contains(platform)));
            }

            return await GetPagedAsync(pageNumber, pageSize, predicate, g => g.Followers, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged games with filters");
            throw;
        }
    }

    public async Task<bool> ExistsByAppIdAsync(ulong appId)
    {
        try
        {
            return await gameRepository.ExistsAsync(g => g.AppId == appId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if game exists with AppId: {AppId}", appId);
            throw;
        }
    }
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(IAnalyticsRepository analyticsRepository, ILogger<AnalyticsService> logger)
    {
        _analyticsRepository = analyticsRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<GenreStatsResponse>> GetTopGenresAsync(string month)
    {
        if (!DateTime.TryParse($"{month}-01", out var monthDate))
            throw new ArgumentException("month must be yyyy-MM");

        var start = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        var stats = await _analyticsRepository.GetGenreAnalyticsAsync(start, end);

        return stats
            .GroupBy(s => s.Genre)
            .Select(g => new GenreStatsResponse
            {
                Genre = g.Key,
                Games = g.Sum(x => x.GameCount),
                AvgFollowers = Math.Round(g.Average(x => x.AvgFollowers))
            })
            .OrderByDescending(x => x.Games)
            .Take(5)
            .ToList();
    }

    public async Task<GenreDynamicsResultResponse> GetDynamicsAsync(string monthsCsv)
    {
        if (string.IsNullOrWhiteSpace(monthsCsv))
            throw new ArgumentException("months required as csv yyyy-MM,...");

        var monthList = monthsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(m => m.Trim())
                                 .OrderBy(m => m)
                                 .ToList();

        // Если месяцев не указано, используем последние 3 месяца как в ТЗ
        if (monthList.Count == 0)
        {
            var now = DateTime.UtcNow;
            monthList = new List<string>
            {
                now.AddMonths(-2).ToString("yyyy-MM"),
                now.AddMonths(-1).ToString("yyyy-MM"),
                now.ToString("yyyy-MM")
            };
            _logger.LogInformation("Using default last 3 months: {Months}", string.Join(", ", monthList));
        }

        var dynamics = await _analyticsRepository.GetGenreDynamicsAsync(monthList);

        // Получаем топ-5 жанров по играм за все периоды
        var topGenres = dynamics
            .GroupBy(d => d.Genre)
            .Select(g => new { Genre = g.Key, TotalGames = g.Sum(x => x.GamesCount) })
            .OrderByDescending(x => x.TotalGames)
            .Take(5)
            .Select(x => x.Genre)
            .ToList();

        _logger.LogInformation("Top 5 genres for dynamics: {Genres}", string.Join(", ", topGenres));

        // Фильтруем динамику только для топ-5 жанров
        var filteredDynamics = dynamics
            .Where(d => topGenres.Contains(d.Genre))
            .ToList();

        var grouped = filteredDynamics.GroupBy(d => d.Genre);

        var series = grouped.Select(g => new GenreDynamicsSeriesResponse
        {
            Genre = g.Key,
            Counts = monthList.Select(m =>
                g.Where(x => x.Month == m).Sum(x => x.GamesCount)
            ).ToList(),
            AvgFollowers = monthList.Select(m =>
            {
                var monthData = g.Where(x => x.Month == m).ToList();
                return monthData.Any()
                    ? (int)Math.Round(monthData.Average(x => x.AvgFollowers))
                    : 0;
            }).ToList()
        }).ToList();

        // Добавляем недостающие жанры с нулевыми значениями
        foreach (var genre in topGenres.Where(genre => !series.Any(s => s.Genre == genre)))
        {
            series.Add(new GenreDynamicsSeriesResponse
            {
                Genre = genre,
                Counts = monthList.Select(_ => 0).ToList(),
                AvgFollowers = monthList.Select(_ => 0).ToList()
            });
        }

        return new GenreDynamicsResultResponse
        {
            Months = monthList,
            Series = series.OrderByDescending(s => s.Counts.Sum()).ToList()
        };
    }

    /// <summary>
    /// НОВЫЙ МЕТОД: Получить динамику за последние 3 месяца 
    /// </summary>
    /// <returns></returns>
    public async Task<GenreDynamicsResultResponse> GetLastThreeMonthsDynamicsAsync()
    {
        var now = DateTime.UtcNow;
        var months = new List<string>
        {
            now.AddMonths(-2).ToString("yyyy-MM"), // сентябрь
            now.AddMonths(-1).ToString("yyyy-MM"), // октябрь  
            now.ToString("yyyy-MM")                // ноябрь
        };

        _logger.LogInformation("Getting dynamics for last 3 months: {Months}", string.Join(", ", months));

        return await GetDynamicsAsync(string.Join(",", months));
    }
}

public class SteamService : ISteamService
{
    private readonly ISteamFollowersService _followersService;
    private readonly IGameRepository _gameRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamService> _logger;

    public SteamService(IGameRepository gameRepository,
        ISteamFollowersService followersService,
        ILogger<SteamService> logger)
    {
        _followersService = followersService;
        _gameRepository = gameRepository;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/118.0.0.0 Safari/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _httpClient.DefaultRequestHeaders.Add("Cookie", "Steam_Language=english");
        _httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");


    }

    /// <summary>
    /// Получение количества подписчиков (followers) через Steam Community Search
    /// </summary>
    private static readonly SemaphoreSlim _semaphore = new(3, 3); // максимум 3 одновременных запроса
    private readonly Dictionary<string, (DateTime fetched, int followers)> _cache = new(); // кеш на 6ч

    private async Task<int> GetFollowersFromCommunityAsync(string appId)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Проверка кеша (если уже брали за последние 6ч)
            if (_cache.TryGetValue(appId, out var cached) && (DateTime.UtcNow - cached.fetched).TotalHours < 6)
            {
                _logger.LogDebug("Cache hit for AppId={AppId}: {Followers}", appId, cached.followers);
                return cached.followers;
            }

            var url = $"https://steamcommunity.com/search/groups/?text={appId}";
            _logger.LogDebug("Requesting community page for AppId={AppId}: {Url}", appId, url);

            var html = await _httpClient.GetStringAsync(url);

            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.LogWarning("Empty response for AppId={AppId}", appId);
                return 0;
            }

            _logger.LogDebug("Received HTML for AppId={AppId}, length={Length}", appId, html.Length);

            // выводим первые 1000 символов HTML для отладки
            _logger.LogDebug("HTML snippet for AppId={AppId}: {Snippet}",
                appId,
                html.Substring(0, Math.Min(1000, html.Length)));

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            // ищем строку результата для нужного appId
            var resultRow = doc.DocumentNode.SelectSingleNode(
                $"//div[contains(@class,'search_row') and .//a[contains(@href,'{appId}')]]");

            if (resultRow == null)
            {
                _logger.LogWarning("No search result found for AppId={AppId}", appId);
                return 0;
            }

            // выводим кусок HTML найденного блока
            _logger.LogDebug("ResultRow HTML for AppId={AppId}: {Snippet}",
                appId,
                resultRow.InnerHtml.Length > 500
                    ? resultRow.InnerHtml.Substring(0, 500) + "..."
                    : resultRow.InnerHtml);

            // ищем span внутри блока searchPersonaInfo
            var countSpan = resultRow.SelectSingleNode(".//div[contains(@class,'searchPersonaInfo')]//span");

            if (countSpan != null)
            {
                var rawText = countSpan.InnerText.Trim();
                _logger.LogDebug("Raw span text for AppId={AppId}: '{Raw}'", appId, rawText);

                if (int.TryParse(rawText, out var followers))
                {
                    _cache[appId] = (DateTime.UtcNow, followers);
                    _logger.LogInformation("✔ Found {Followers} followers for AppId={AppId}", followers, appId);
                    return followers;
                }

                var digits = Regex.Match(rawText, @"\\d+").Value;
                _logger.LogDebug("Regex digits from span for AppId={AppId}: '{Digits}'", appId, digits);

                if (int.TryParse(digits, out followers))
                {
                    _cache[appId] = (DateTime.UtcNow, followers);
                    _logger.LogInformation("✔ Found {Followers} followers for AppId={AppId}", followers, appId);
                    return followers;
                }
            }
            else
            {
                _logger.LogDebug("No <span> found inside searchPersonaInfo for AppId={AppId}", appId);
            }

            // fallback: ищем любое число в тексте всей строки
            var rowText = (resultRow.InnerText ?? "").Trim();
            _logger.LogDebug("Fallback row text for AppId={AppId}: '{RowText}'", appId, rowText);

            var digitsFallback = Regex.Match(rowText, @"\\b\\d+\\b");
            if (digitsFallback.Success && int.TryParse(digitsFallback.Value, out var followersFallback))
            {
                _cache[appId] = (DateTime.UtcNow, followersFallback);
                _logger.LogInformation("✔ Found (fallback) {Followers} followers for AppId={AppId}", followersFallback, appId);
                return followersFallback;
            }

            _logger.LogWarning("Followers not found in HTML for AppId={AppId}", appId);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while parsing followers for AppId={AppId}", appId);
            return 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Поиск upcoming игр через Steam Store Search
    /// </summary>
    private const string SteamSearchUrl =
        "https://store.steampowered.com/search/?sort_by=Released_DESC&category1=998&ndl=1&page={0}";

    private async Task<List<SteamSearchResult>> GetUpcomingAppIdsAsync(DateTime startDate, DateTime endDate)
    {
        var results = new List<SteamSearchResult>();
        int page = 0;
        bool hasMore = true;

        while (hasMore && page < 3)
        {
            page++;
            var url = string.Format(SteamSearchUrl, page);
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                await Task.Delay(2000);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var rows = doc.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");
                if (rows == null || !rows.Any()) break;

                foreach (var row in rows)
                {
                    var dataDsAppid = row.GetAttributeValue("data-ds-appid", "");
                    if (string.IsNullOrEmpty(dataDsAppid)) continue;

                    var releaseNode = row.SelectSingleNode(".//div[contains(@class, 'search_released')]");
                    var releaseText = releaseNode?.InnerText.Trim() ?? "";
                    var releaseDate = ParseReleaseDate(releaseText);
                    if (!releaseDate.HasValue) continue;

                    if (releaseDate.Value >= startDate && releaseDate.Value <= endDate)
                    {
                        var appIdString = dataDsAppid.Split(',')[0];

                        // Конвертируем string в ulong
                        if (ulong.TryParse(appIdString, out ulong appId))
                        {
                            results.Add(new SteamSearchResult
                            {
                                AppId = appId,  // ← теперь ulong
                                ReleaseDate = releaseDate.Value
                            });
                        }
                        else
                        {
                            _logger.LogWarning("Failed to parse AppId: {AppIdString}", appIdString);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге страницы {Page}", page);
                break;
            }
        }

        _logger.LogInformation("Found {Count} upcoming games", results.Count);
        return results;
    }

    private static readonly Regex _monthYearRegex = new(@"(\w+)\s+(\d{4})", RegexOptions.Compiled);
    private static readonly Regex _yearOnlyRegex = new(@"\b(20\d{2})\b", RegexOptions.Compiled);
    private static readonly string[] _months =
    {
        "Jan","Feb","Mar","Apr","May","Jun",
        "Jul","Aug","Sep","Oct","Nov","Dec"
    };

    private static DateTime? ParseReleaseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text.Contains("TBA", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Coming Soon", StringComparison.OrdinalIgnoreCase))
            return null;

        var match = _monthYearRegex.Match(text);
        if (match.Success)
        {
            var monthName = match.Groups[1].Value;
            var year = int.Parse(match.Groups[2].Value);
            var month = Array.IndexOf(_months, monthName) + 1;
            if (month > 0) return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        var yearMatch = _yearOnlyRegex.Match(text);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);
            return new DateTime(year, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        }

        if (DateTime.TryParse(text, out var parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return null;
    }

    /// <summary>
    /// Синхронизация игр
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public async Task SyncUpcomingGamesAsync(DateTime startDate, DateTime endDate)
    {
        var upcomingList = await GetUpcomingAppIdsAsync(startDate, endDate);
        if (!upcomingList.Any())
        {
            _logger.LogInformation("No upcoming games found between {Start} and {End}", startDate, endDate);
            return;
        }

        foreach (var gameInfo in upcomingList)
        {
            try
            {
                _logger.LogInformation("Processing game {AppId} with release date {ReleaseDate}", gameInfo.AppId, gameInfo.ReleaseDate);

                var game = await GetGameDetailsAsync(gameInfo.AppId, gameInfo.ReleaseDate);
                if (game == null)
                {
                    _logger.LogWarning("No details found for AppId={AppId}", gameInfo.AppId);
                    continue;
                }

                var existing = await _gameRepository.GetByAppIdAsync(game.AppId);

                if (existing == null
                    || existing.CollectedAt < DateTime.UtcNow.AddDays(-1)
                    || existing.Followers == 0)
                {
                    var followers = await _followersService.GetFollowersAsync(game.AppId);
                    game.Followers = followers;
                    game.CollectedAt = DateTime.UtcNow;

                    _logger.LogInformation("✔ AppId={AppId}, Name={Name}, Followers={Followers}", game.AppId, game.Name, followers);
                }
                else
                {
                    game.Followers = existing.Followers;
                    game.CollectedAt = existing.CollectedAt;

                    _logger.LogInformation("↺ AppId={AppId}, Name={Name}, using cached Followers={Followers}", game.AppId, game.Name, existing.Followers);
                }

                if (existing != null)
                    await UpdateExistingGame(existing, game);
                else
                    await _gameRepository.AddAsync(game);

                await Task.Delay(1000); // пауза между запросами
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing the game {AppId}", gameInfo.AppId);
            }
        }

        await _gameRepository.SaveChangesAsync();
        _logger.LogInformation("Saved all game updates to the database");
    }

    /// <summary>
    /// Детали игры
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="releaseDate"></param>
    /// <returns></returns>
    private async Task<Game?> GetGameDetailsAsync(ulong appId, DateTime? releaseDate)
    {
        var detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}";
        try
        {
            var json = await _httpClient.GetStringAsync(detailsUrl);
            var detailsDict = JsonSerializer.Deserialize<Dictionary<ulong, AppDetails>>(json);
            if (detailsDict == null || !detailsDict.TryGetValue(appId, out var appDetails) || !appDetails.success)
                return null;

            var data = appDetails.data;
            DateTime? preciseDate = null;
            if (DateTime.TryParse(data.release_date?.date, out var pd))
                preciseDate = DateTime.SpecifyKind(pd, DateTimeKind.Utc);

            var finalReleaseDate = preciseDate ?? releaseDate;
            if (!finalReleaseDate.HasValue) return null;

            return new Game
            {
                AppId = appId,
                Name = data.name?.Trim() ?? "Unknown",
                ReleaseDate = finalReleaseDate,
                Genres = data.genres?.Select(g => g.description.Trim()).ToList() ?? new List<string>(),
                ShortDescription = data.short_description?.Trim() ?? "",
                PosterUrl = data.header_image?.Trim() ?? "",
                Platforms = GetPlatforms(data.platforms),
                StoreUrl = $"https://store.steampowered.com/app/{appId}/",
                Followers = 0, // обновим позже в SyncUpcomingGamesAsync
                CollectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении деталей для {AppId}", appId);
            return null;
        }
    }

    private static List<string> GetPlatforms(Platforms platforms)
    {
        var result = new List<string>();
        if (platforms?.windows == true) result.Add("Windows");
        if (platforms?.mac == true) result.Add("Mac");
        if (platforms?.linux == true) result.Add("Linux");

        return result;
    }

    // -------------------------------------------------------------
    // Обновление существующей игры
    // -------------------------------------------------------------
    private async Task UpdateExistingGame(Game existing, Game newData)
    {
        existing.Name = newData.Name;
        existing.ReleaseDate = newData.ReleaseDate;
        existing.Genres = newData.Genres;
        existing.ShortDescription = newData.ShortDescription;
        existing.PosterUrl = newData.PosterUrl;
        existing.Platforms = newData.Platforms;
        existing.StoreUrl = newData.StoreUrl;

        // Followers обновляем только если новые данные свежее и > 0
        if (newData.CollectedAt > existing.CollectedAt && newData.Followers > 0)
        {
            existing.Followers = newData.Followers;
            existing.CollectedAt = newData.CollectedAt;
        }

        await _gameRepository.UpdateAsync(existing);
    }

    // -------------------------------------------------------------
    // Публичные методы для API
    // -------------------------------------------------------------
    public async Task<IEnumerable<Game>> GetReleasesAsync(string month)
    {
        var date = DateTime.Parse(month + "-01");
        return await _gameRepository.GetGamesByMonthAsync(date.Year, date.Month);
    }

    public async Task<IEnumerable<object>> GetCalendarAsync(string month)
    {
        var date = DateTime.Parse(month + "-01");
        var games = await _gameRepository.GetGamesByMonthAsync(date.Year, date.Month);

        var calendar = games.GroupBy(g => g.ReleaseDate?.Date)
            .Select(group => new { date = group.Key?.ToString("yyyy-MM-dd"), count = group.Count() })
            .ToList<object>();

        return calendar;
    }

    public async Task<object> GetDynamicsAsync(string monthsCsv)
    {
        if (string.IsNullOrWhiteSpace(monthsCsv))
            throw new ArgumentException("months required as csv yyyy-MM,...", nameof(monthsCsv));

        var monthList = monthsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(m => m.Trim())
            .OrderBy(m => m)
            .ToList();

        var ranges = monthList.Select(m =>
        {
            if (!DateTime.TryParse($"{m}-01", out var parsed))
                throw new ArgumentException($"Invalid month format: {m}");
            var start = new DateTime(parsed.Year, parsed.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = start.AddMonths(1).AddTicks(-1);
            return (Month: m, Start: start, End: end);
        }).ToList();

        var result = new List<object>();
        foreach (var range in ranges)
        {
            var games = await _gameRepository.GetGamesByDateRangeAsync(range.Start, range.End);
            var gameList = games.ToList();

            ulong avgFollowers = 0;
            if (gameList.Count > 0)
            {
                double totalFollowers = gameList.Sum(g => (double)g.Followers);
                double average = totalFollowers / gameList.Count;
                avgFollowers = (ulong)Math.Round(average);
            }

            result.Add(new
            {
                month = range.Month,
                count = gameList.Count,
                avgFollowers = avgFollowers
            });
        }

        return result;
    }

    public async Task<IEnumerable<object>> GetTopGenresAsync(string month)
    {
        if (string.IsNullOrWhiteSpace(month))
            throw new ArgumentException("month is required (yyyy-MM)", nameof(month));

        if (!DateTime.TryParse($"{month}-01", out var monthDate))
            throw new ArgumentException("month must be in format yyyy-MM", nameof(month));

        var startDate = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1).AddTicks(-1);

        var games = await _gameRepository.GetGamesByDateRangeAsync(startDate, endDate);

        var stats = games
            .SelectMany(g => g.Genres.Select(genre => new { Genre = genre, Game = g }))
            .GroupBy(x => x.Genre)
            .Select(g =>
            {
                double avgValue = g.Any() ? g.Average(x => (double)x.Game.Followers) : 0;
                ulong avgFollowers = avgValue >= 0 ? (ulong)Math.Round(avgValue) : 0;

                return new
                {
                    genre = g.Key,
                    games = g.Count(),
                    avgFollowers = avgFollowers
                };
            })
            .OrderByDescending(x => x.games)
            .Take(5);

        return stats.ToList();
    }
}

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(string username)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public class SteamSyncService : ISteamSyncService
{
    private readonly IServiceProvider _serviceProvider;

    private readonly ILogger<SteamSyncService> _logger;
    public SteamSyncService(
        IServiceProvider serviceProvider,
        ILogger<SteamSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Steam data synchronization...");

        using var scope = _serviceProvider.CreateScope();
        var steamService = scope.ServiceProvider.GetRequiredService<ISteamService>();
        var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>(); // Репозиторий PostgreSQL
        var analyticsRepository = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>(); // Репозиторий ClickHouse

        var now = DateTime.UtcNow;
        var startDate = now.AddYears(-1);
        var endDate = now.AddYears(1);

        _logger.LogInformation("Sync date range: {StartDate} to {EndDate}", startDate, endDate);

        // Запускаем синхронизацию Steam
        _logger.LogInformation("Starting Steam service synchronization...");
        await steamService.SyncUpcomingGamesAsync(startDate, endDate);
        _logger.LogInformation("Steam service synchronization completed");

        // Получаем данные из PostgreSQL
        _logger.LogInformation("Retrieving games from PostgreSQL database...");
        var games = await gameRepository.GetGamesByDateRangeAsync(startDate, endDate);

        _logger.LogInformation("Found {GameCount} games in PostgreSQL for analytics", games.Count());

        if (games.Any())
        {
            _logger.LogInformation("First game sample: AppId={FirstAppId}, Name={FirstName}",
                games.First().AppId, games.First().Name);
        }

        // Сохраняем в ClickHouse
        await SaveGamesToAnalyticsAsync(games, analyticsRepository);

        _logger.LogInformation("Steam data synchronization completed successfully");
    }

    private async Task SaveGamesToAnalyticsAsync(IEnumerable<Game> games, IAnalyticsRepository analyticsRepository)
    {
        if (games == null || !games.Any())
        {
            _logger.LogWarning("No games to save to analytics");
            return;
        }

        _logger.LogInformation("Starting analytics storage for {GameCount} games to ClickHouse", games.Count());

        // DEV DEBUG: Validate data compatibility
        var gameList = games.ToList();
        _logger.LogDebug("DEV - Data compatibility check:");

        // В методе SaveGamesToAnalyticsAsync обновите логи валидации:
        foreach (var game in gameList.Take(3))
        {
            _logger.LogDebug("DEV - Game validation: AppId={AppId}, " +
                             "ReleaseDate={ReleaseDate} (has value: {HasReleaseDate}), " +
                             "Genres count={GenresCount}, Followers={Followers}, " +
                             "Platforms count={PlatformsCount}, CollectedAt={CollectedAt}",
                game.AppId,  // ← теперь просто ulong, без конвертации
                game.ReleaseDate?.ToString("yyyy-MM-dd") ?? "NULL",
                game.ReleaseDate.HasValue,
                game.Genres?.Count ?? 0,
                game.Followers,
                game.Platforms?.Count ?? 0,
                game.CollectedAt);
        }
        int successCount = 0;
        int errorCount = 0;
        var failedGames = new List<(ulong AppId, string Error)>();

        foreach (var game in gameList)
        {
            try
            {
                // DEV DEBUG: Pre-storage validation
                var validationErrors = ValidateGameForClickHouse(game);
                if (validationErrors.Any())
                {
                    _logger.LogWarning("DEV - Game validation failed for AppId={AppId}: {Errors}",
                        game.AppId, string.Join("; ", validationErrors));
                }

                _logger.LogDebug("Storing game in analytics: AppId={AppId}, Name={Name}, Genres={GenresCount}",
                    game.AppId, game.Name, game.Genres?.Count ?? 0);

                await analyticsRepository.StoreGameAnalyticsAsync(game);
                successCount++;

                _logger.LogDebug("Successfully stored game in analytics: AppId={AppId}", game.AppId);

                // Progress tracking
                if (successCount % 10 == 0)
                {
                    _logger.LogInformation("DEV - Storage progress: {SuccessCount}/{TotalCount} games processed",
                        successCount, gameList.Count);
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                failedGames.Add((game.AppId, ex.Message));

                _logger.LogError(ex, "Failed to store game in analytics: AppId={AppId}, Name={Name}",
                    game.AppId, game.Name);

                // Detailed error analysis
                _logger.LogDebug("DEV - Error analysis for AppId={AppId}: {ErrorType} - {ErrorMessage}",
                    game.AppId, ex.GetType().Name, ex.Message);

                // Specific handling for common issues
                if (ex.Message.Contains("AppId") || ex.Message.Contains("parsing"))
                {
                    _logger.LogWarning("DEV - Possible data type issue with AppId: '{AppId}'", game.AppId);
                }
            }
        }

        // Final summary with detailed analytics
        _logger.LogInformation("Analytics storage completed: {SuccessCount} successful, {ErrorCount} failed",
            successCount, errorCount);

        _logger.LogInformation("DEV - Detailed summary: Total={Total}, Success={Success} ({SuccessRate:P2}), Failed={Failed}",
            gameList.Count, successCount, (double)successCount / gameList.Count, errorCount);

        if (errorCount > 0)
        {
            _logger.LogWarning("Some games failed to save to analytics: {ErrorCount} errors", errorCount);

            _logger.LogWarning("DEV - Failed games analysis (first 5):");
            foreach (var failed in failedGames.Take(5))
            {
                _logger.LogWarning("DEV - Failed: AppId={AppId}, Error={Error}", failed.AppId, failed.Error);
            }
        }
        else
        {
            _logger.LogInformation("DEV - SUCCESS: All {GameCount} games stored in ClickHouse analytics", gameList.Count);
        }
    }

    private List<string> ValidateGameForClickHouse(Game game)
    {
        var errors = new List<string>();

        // AppId теперь ulong - проверка не нужна, так как он всегда валидный ulong
        // Убедитесь, что AppId не равен 0 (значение по умолчанию для ulong)
        if (game.AppId == 0)
        {
            errors.Add("AppId cannot be 0");
        }

        // Check required fields
        if (string.IsNullOrWhiteSpace(game.Name))
        {
            errors.Add("Game name is empty");
        }

        if (game.Genres == null || !game.Genres.Any())
        {
            errors.Add("No genres specified");
        }

        // Проверка followers не нужна, так как ulong не может быть отрицательным
        // ulong всегда >= 0 по определению

        // Дополнительные проверки для ClickHouse
        if (game.ReleaseDate == null)
        {
            errors.Add("ReleaseDate is required for ClickHouse");
        }

        if (game.CollectedAt == default)
        {
            errors.Add("CollectedAt is required for ClickHouse");
        }

        return errors;
    }
}

public class SteamBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SteamBackgroundService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6);

    public SteamBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SteamBackgroundService> logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISteamSyncService>();

                await syncService.SyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Steam data synchronization");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }
}

/// <summary>
/// Вспомогательные классы для десериализации Steam API
/// </summary>
public class AppListResponse
{
    public required AppList applist { get; set; }
}

public class AppList
{
    public required List<App> apps { get; set; }
}

public class App
{
    public int Appid { get; set; }
    public required string name { get; set; }
}

public class AppDetails
{
    public bool success { get; set; }
    public required AppData data { get; set; }
}

public class AppData
{
    public required string name { get; set; }
    public required ReleaseDate release_date { get; set; }
    public required string short_description { get; set; }
    public required string header_image { get; set; }
    public required Platforms platforms { get; set; }
    public required List<Genre> genres { get; set; }
}

public class ReleaseDate
{
    public bool coming_soon { get; set; }
    public required string date { get; set; }
}

public class Platforms
{
    public bool windows { get; set; }
    public bool mac { get; set; }
    public bool linux { get; set; }
}

public class Genre
{
    public required string description { get; set; }
}