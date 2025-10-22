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

namespace GameReleases.Core.Services;

public abstract class Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    : IServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    where TEntity : class
    where TCreateRequest : class
    where TUpdateRequest : class
    where TResponse : class
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly ILogger _logger;

    protected Services(IRepository<TEntity> repository, ILogger<Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // Abstract methods for mapping
    protected abstract TEntity MapToEntity(TCreateRequest request);
    protected abstract TResponse MapToResponse(TEntity entity);
    protected abstract void UpdateEntity(TEntity entity, TUpdateRequest request);

    // Read
    public virtual async Task<TResponse?> GetByIdAsync(TId id)
    {
        try
        {
            // TODO: доделать проверки на null
            var entity = await _repository.GetByIdAsync((Guid)(object)id);
            return entity != null ? MapToResponse(entity) : null;
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

    // Pagination
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

    // Create
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

    // Update
    public virtual async Task<TResponse?> UpdateAsync(TId id, TUpdateRequest request)
    {
        try
        {
            // TODO: доделать проверки на null

            var entity = await _repository.GetByIdAsync((Guid)(object)id);
            if (entity == null)
            {
                return null;
            }

            UpdateEntity(entity, request);
            _repository.Update(entity);
            await _repository.SaveChangesAsync();

            return MapToResponse(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityName} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    // Delete
    public virtual async Task<bool> DeleteAsync(TId id)
    {
        try
        {
            // TODO: доделать проверки на null

            var entity = await _repository.GetByIdAsync((Guid)(object)id);
            if (entity == null)
            {
                return false;
            }

            _repository.Remove(entity);
            await _repository.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityName} with ID: {Id}", typeof(TEntity).Name, id);
            throw;
        }
    }

    // Utility
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

public class GameService(
    IGameRepository gameRepository,
    ILogger<GameService> logger)
    : Services<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>(gameRepository, logger), IGameService
{
    // Реализация абстрактных методов маппинга
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

    // Переопределение CreateAsync для дополнительной валидации
    public override async Task<GameResponse> CreateAsync(CreateGameRequest request)
    {
        // Проверка на существование игры с таким AppId
        var existingGame = await gameRepository.GetByAppIdAsync(request.AppId);
        if (existingGame != null)
        {
            throw new InvalidOperationException($"Game with AppId '{request.AppId}' already exists");
        }

        return await base.CreateAsync(request);
    }

    // Специфичные методы для Game
    public async Task<GameResponse?> GetByAppIdAsync(string appId)
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

    public async Task<bool> ExistsByAppIdAsync(string appId)
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

public class SteamService : ISteamService
{
    private readonly IGameRepository _gameRepository;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SteamService> _logger;

    public SteamService(IGameRepository gameRepository, ILogger<SteamService> logger)
    {
        _gameRepository = gameRepository;
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GameReleasesBot/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }


    //-------------------------------------------------------------
    // Парсинг количества подписчиков(followers) со страницы магазина
    //-------------------------------------------------------------


    private const string SteamSearchUrl = "https://store.steampowered.com/search/?sort_by=Released_DESC&category1=998&ndl=1&page={0}";

    /// <summary>
    /// Парсит страницы поиска Steam, собирает appId и дату релиза всех upcoming‑игр.
    /// Останавливается, когда встречает игру с датой после endDate: после ноября 2025.
    /// </summary>
    private async Task<List<SteamSearchResult>> GetUpcomingAppIdsAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("🔍 Searching Steam for games from {StartDate:yyyy-MM} to {EndDate:yyyy-MM}",
            startDate, endDate);

        var results = new List<SteamSearchResult>();
        int page = 0;
        bool hasMore = true;
        int totalRowsProcessed = 0;

        while (hasMore && page < 10) // Ограничим 10 страницами для теста
        {
            page++;
            var url = string.Format(SteamSearchUrl, page);
            _logger.LogInformation("📄 Fetching page {Page}: {Url}", page, url);

            try
            {
                var html = await _httpClient.GetStringAsync(url);
                _logger.LogDebug("✅ Successfully fetched page {Page}, HTML length: {Length} chars", page, html.Length);

                await Task.Delay(2000); // Увеличим паузу

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Ищем строки с играми
                var rows = doc.DocumentNode.SelectNodes("//a[contains(@class, 'search_result_row')]");

                if (rows == null || !rows.Any())
                {
                    _logger.LogWarning("❌ No game rows found on page {Page}. HTML might have different structure.", page);

                    // Дамп небольшой части HTML для отладки
                    if (html.Length > 500)
                    {
                        _logger.LogDebug("First 500 chars of HTML: {HtmlSample}", html.Substring(0, 500));
                    }
                    break;
                }

                _logger.LogInformation("🎮 Found {RowCount} game rows on page {Page}", rows.Count, page);
                totalRowsProcessed += rows.Count;

                bool foundGameInRange = false;

                foreach (var row in rows)
                {
                    try
                    {
                        // Получаем appId
                        var dataDsAppid = row.GetAttributeValue("data-ds-appid", "");
                        var dataDsBundleid = row.GetAttributeValue("data-ds-bundleid", "");
                        var dataDsPackageid = row.GetAttributeValue("data-ds-packageid", "");

                        _logger.LogDebug("📦 AppID: {AppId}, Bundle: {Bundle}, Package: {Package}",
                            dataDsAppid, dataDsBundleid, dataDsPackageid);

                        if (string.IsNullOrEmpty(dataDsAppid))
                        {
                            _logger.LogDebug("⏩ Skipping row with empty appId");
                            continue;
                        }

                        // Получаем дату релиза
                        var releaseNode = row.SelectSingleNode(".//div[contains(@class, 'search_released')]");
                        var releaseText = releaseNode?.InnerText.Trim() ?? "No release date";

                        _logger.LogDebug("📅 Release text: '{ReleaseText}'", releaseText);

                        var releaseDate = ParseReleaseDate(releaseText);

                        if (!releaseDate.HasValue)
                        {
                            _logger.LogDebug("⏩ Skipping - cannot parse release date");
                            continue;
                        }

                        _logger.LogDebug("✅ Parsed release date: {ParsedDate}", releaseDate);

                        // Проверяем диапазон дат
                        if (releaseDate.Value > endDate)
                        {
                            _logger.LogDebug("⏩ Date {ReleaseDate} beyond end date {EndDate}", releaseDate, endDate);
                            continue;
                        }

                        if (releaseDate.Value >= startDate && releaseDate.Value <= endDate)
                        {
                            var appId = dataDsAppid.Split(',')[0]; // Берем первый appId
                            results.Add(new SteamSearchResult
                            {
                                AppId = appId,
                                ReleaseDate = releaseDate.Value
                            });
                            foundGameInRange = true;
                            _logger.LogInformation("🎯 ADDED: AppID {AppId} with date {ReleaseDate:yyyy-MM}",
                                appId, releaseDate.Value);
                        }
                        else
                        {
                            _logger.LogDebug("⏩ Date {ReleaseDate} outside range", releaseDate);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing game row");
                    }
                }

                _logger.LogInformation("📊 Page {Page} completed. Results so far: {ResultsCount}", page, results.Count);

                // Если на странице не нашли игр в диапазоне, останавливаемся
                if (!foundGameInRange && rows.Any())
                {
                    _logger.LogInformation("🛑 No games in date range found on page {Page}, stopping search", page);
                    break;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching page {Page}", page);
                break;
            }
        }

        _logger.LogInformation("🎉 Search completed: {TotalRows} rows processed, {TotalResults} games found",
            totalRowsProcessed, results.Count);

        return results;
    }
    private static readonly Regex _monthYearRegex = new(@"(\w+)\s+(\d{4})", RegexOptions.Compiled);
    private static readonly Regex _yearOnlyRegex = new(@"\b(20\d{2})\b", RegexOptions.Compiled);
    private static readonly string[] _months =
    [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    private static DateTime? ParseReleaseDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            text.Contains("TBA", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Coming Soon", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("To be announced", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        text = text.Trim();

        // "Nov 2025" - точная дата месяца
        var match = _monthYearRegex.Match(text);
        if (match.Success)
        {
            var monthName = match.Groups[1].Value;
            var year = int.Parse(match.Groups[2].Value);
            var month = Array.IndexOf(_months, monthName) + 1;
            if (month > 0)
                return new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        // "2025" - только год
        var yearMatch = _yearOnlyRegex.Match(text);
        if (yearMatch.Success)
        {
            var year = int.Parse(yearMatch.Value);
            if (year >= 2024 && year <= 2030)
                return new DateTime(year, 6, 15, 0, 0, 0, DateTimeKind.Utc); // Середина года
        }

        // Кварталы "Q4 2025"
        if (text.Contains("Q4", StringComparison.OrdinalIgnoreCase) || text.Contains("4 Quarter", StringComparison.OrdinalIgnoreCase))
        {
            var year = ExtractYear(text);
            return year.HasValue ? new DateTime(year.Value, 10, 1, 0, 0, 0, DateTimeKind.Utc) : null;
        }
        if (text.Contains("Q3", StringComparison.OrdinalIgnoreCase) || text.Contains("3 Quarter", StringComparison.OrdinalIgnoreCase))
        {
            var year = ExtractYear(text);
            return year.HasValue ? new DateTime(year.Value, 7, 1, 0, 0, 0, DateTimeKind.Utc) : null;
        }
        if (text.Contains("Q2", StringComparison.OrdinalIgnoreCase) || text.Contains("2 Quarter", StringComparison.OrdinalIgnoreCase))
        {
            var year = ExtractYear(text);
            return year.HasValue ? new DateTime(year.Value, 4, 1, 0, 0, 0, DateTimeKind.Utc) : null;
        }
        if (text.Contains("Q1", StringComparison.OrdinalIgnoreCase) || text.Contains("1 Quarter", StringComparison.OrdinalIgnoreCase))
        {
            var year = ExtractYear(text);
            return year.HasValue ? new DateTime(year.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc) : null;
        }

        // Пытаемся распарсить как обычную дату
        if (DateTime.TryParse(text, out var parsedDate))
        {
            return DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }

        return null;
    }

    private static int? ExtractYear(string text)
    {
        var match = _yearOnlyRegex.Match(text);
        return match.Success ? int.Parse(match.Value) : null;
    }
    public async Task SyncUpcomingGamesAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("🔄 Starting Steam sync for {StartDate:yyyy-MM} to {EndDate:yyyy-MM}",
            startDate, endDate);

        try
        {
            // 1. Поиск upcoming игр через Steam Search
            _logger.LogInformation("🔍 Searching for upcoming games...");
            var upcomingList = await GetUpcomingAppIdsAsync(startDate, endDate);

            if (!upcomingList.Any())
            {
                _logger.LogWarning("❌ No upcoming games found in date range");
                return;
            }

            _logger.LogInformation("📊 Found {Count} upcoming games", upcomingList.Count);

            // 2. ⭐ ИЗМЕНЕНИЕ: Обрабатываем игры по 10 за раз (вместо 100)
            var appIdChunks = upcomingList.Chunk(10); // Уменьшили с 100 до 10
            int totalProcessed = 0;
            int totalAdded = 0;
            int totalUpdated = 0;
            int errorCount = 0;

            foreach (var chunk in appIdChunks)
            {
                _logger.LogDebug("Processing chunk of {ChunkSize} games", chunk.Length);

                // Обрабатываем каждую игру отдельно
                foreach (var gameInfo in chunk)
                {
                    try
                    {
                        _logger.LogDebug("🔄 Processing game {AppId}", gameInfo.AppId);

                        // Запрашиваем детали для ОДНОЙ игры
                        var game = await GetGameDetailsAsync(gameInfo.AppId, gameInfo.ReleaseDate);
                        if (game == null)
                        {
                            errorCount++;
                            continue;
                        }

                        // Сохранение в базу
                        var existing = await _gameRepository.GetByAppIdAsync(game.AppId);
                        if (existing != null)
                        {
                            await UpdateExistingGame(existing, game);
                            totalUpdated++;
                            _logger.LogDebug("✅ Updated game: {GameName}", game.Name);
                        }
                        else
                        {
                            await _gameRepository.AddAsync(game);
                            totalAdded++;
                            _logger.LogInformation("🎯 Added new game: {GameName}", game.Name);
                        }
                        totalProcessed++;

                        // ПАУЗА между запросами
                        await Task.Delay(1500);
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex, "❌ Error processing game {AppId}", gameInfo.AppId);
                    }
                }
            }

            await _gameRepository.SaveChangesAsync();

            _logger.LogInformation("🎉 Sync completed: Processed={Processed}, Added={Added}, Updated={Updated}, Errors={Errors}",
                totalProcessed, totalAdded, totalUpdated, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Steam sync failed");
            throw;
        }
    }

    /// <summary>
    /// МЕТОД ДЛЯ ПОЛУЧЕНИЯ ДЕТАЛЕЙ ОДНОЙ ИГРЫ
    /// </summary>
    /// <param name="appId"></param>
    /// <param name="releaseDate"></param>
    /// <returns></returns>
    private async Task<Game?> GetGameDetailsAsync(string appId, DateTime? releaseDate)
    {
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}";
                _logger.LogDebug("🌐 Fetching details for {AppId}", appId);

                var json = await _httpClient.GetStringAsync(detailsUrl);

                var detailsDict = JsonSerializer.Deserialize<Dictionary<string, AppDetails>>(json);
                if (detailsDict == null || !detailsDict.TryGetValue(appId, out var appDetails))
                {
                    _logger.LogWarning("❌ Failed to deserialize response for {AppId}", appId);
                    return null;
                }

                if (!appDetails.success)
                {
                    _logger.LogDebug("⏩ App {AppId} not successful in API", appId);
                    return null;
                }

                var data = appDetails.data;

                // Определяем дату релиза
                DateTime? preciseDate = null;
                if (DateTime.TryParse(data.release_date?.date, out var pd))
                {
                    preciseDate = DateTime.SpecifyKind(pd, DateTimeKind.Utc);
                    _logger.LogDebug("📅 Parsed precise date for {AppId}: {Date}", appId, preciseDate);
                }

                // ИСПОЛЬЗУЕМ releaseDate.Value ИЛИ preciseDate
                var finalReleaseDate = preciseDate ?? releaseDate;

                if (!finalReleaseDate.HasValue)
                {
                    _logger.LogWarning("❌ No release date found for {AppId}", appId);
                    return null;
                }

                var game = new Game
                {
                    AppId = appId,
                    Name = data.name?.Trim() ?? "Unknown",
                    ReleaseDate = preciseDate ?? releaseDate,
                    Genres = data.genres?.Select(g => g.description.Trim()).ToHashSet() ?? [],
                    ShortDescription = data.short_description?.Trim() ?? "",
                    PosterUrl = data.header_image?.Trim() ?? "",
                    Platforms = GetPlatforms(data.platforms),
                    StoreUrl = $"https://store.steampowered.com/app/{appId}/",
                    Followers = await GetFollowersAsync(appId),
                    CollectedAt = DateTime.UtcNow
                };

                _logger.LogDebug("✅ Created game: {Name} ({AppId})", game.Name, game.AppId);

                await Task.Delay(500); // 2 секунды между запросами
                return game;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                _logger.LogWarning("⚠️ Attempt {Attempt} failed for {AppId}, retrying...", attempt, appId);
                await Task.Delay(2000 * attempt); // Увеличивающаяся пауза
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting details for {AppId}", appId);
                return null;
            }
        }

        return null;
    }

    private async Task<Game?> CreateGameFromDetails(string appId, AppData data, SteamSearchResult[] chunk)
    {
        try
        {
            var searchResult = chunk.FirstOrDefault(x => x.AppId == appId);
            if (searchResult == null) return null;

            // Определяем дату релиза
            DateTime? preciseDate = null;
            if (DateTime.TryParse(data.release_date?.date, out var pd))
            {
                preciseDate = DateTime.SpecifyKind(pd, DateTimeKind.Utc);
            }

            var game = new Game
            {
                AppId = appId,
                Name = data.name?.Trim() ?? "Unknown",
                ReleaseDate = preciseDate ?? searchResult.ReleaseDate,
                Genres = data.genres?.Select(g => g.description.Trim()).ToHashSet() ?? [],
                ShortDescription = data.short_description?.Trim() ?? "",
                PosterUrl = data.header_image?.Trim() ?? "",
                Platforms = GetPlatforms(data.platforms),
                StoreUrl = $"https://store.steampowered.com/app/{appId}/",
                Followers = await GetFollowersAsync(appId),
                CollectedAt = DateTime.UtcNow
            };

            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating game from details for {AppId}", appId);
            return null;
        }
    }

    private static HashSet<string> GetPlatforms(Platforms platforms)
    {
        var result = new HashSet<string>();
        if (platforms?.windows == true) result.Add("Windows");
        if (platforms?.mac == true) result.Add("Mac");
        if (platforms?.linux == true) result.Add("Linux");
        return result;
    }

    private async Task UpdateExistingGame(Game existing, Game newData)
    {
        existing.Name = newData.Name;
        existing.ReleaseDate = newData.ReleaseDate;
        existing.Genres = newData.Genres;
        existing.Followers = newData.Followers;
        existing.ShortDescription = newData.ShortDescription;
        existing.PosterUrl = newData.PosterUrl;
        existing.Platforms = newData.Platforms;
        existing.StoreUrl = newData.StoreUrl;
        existing.CollectedAt = newData.CollectedAt;

        await _gameRepository.UpdateAsync(existing);
    }

    private async Task<int> GetFollowersAsync(string appId)
    {
        const int maxRetries = 3;
        const int delayMs = 1500;   // небольшая пауза между запросами – уважение к Steam

        var url = $"https://store.steampowered.com/app/{appId}/";

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 1. Получаем HTML страницы
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var html = await response.Content.ReadAsStringAsync();

                // 2. Ищем нужный кусок
                //    В 2025‑м Steam всё ещё кладёт followers в JSON‑блок внутри <script type="text/javascript">
                //    Пример строки:
                //    "followers":123456,
                var match = Regex.Match(html,
                    @"""followers""\s*:\s*(\d+)",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (match.Success && int.TryParse(match.Groups[1].Value, out var followers))
                {
                    // небольшая задержка – не спамим
                    await Task.Delay(delayMs);
                    return followers;
                }

                // Если регулярка не сработала – пробуем HtmlAgilityPack (запасной вариант)
                return await ParseFollowersWithHap(html);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                // Логируем 
                _logger.LogError($"[Attempt {attempt}] HTTP error for app {appId}: {ex.Message}");
                await Task.Delay(delayMs * attempt);
            }
            catch (TaskCanceledException) when (attempt < maxRetries)
            {
                await Task.Delay(delayMs * attempt);
            }
        }

        // Если всё упало – возвращаем 0 (заглушка)
        return -1;
    }

    // --------------------------------------------------------------------
    //  Запасной парсер через HtmlAgilityPack (на случай изменения структуры)
    // --------------------------------------------------------------------
    private static Task<int> ParseFollowersWithHap(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // На странице есть блок <div class="glance_ctn"> → внутри <div class="follower_stats">
        var followerNode = doc.DocumentNode
            .SelectSingleNode("//div[contains(@class,'follower_stats')]//span[@class='count']");

        if (followerNode != null &&
            int.TryParse(Regex.Replace(followerNode.InnerText, @"[^\d]", ""), out var followers))
        {
            return Task.FromResult(followers);
        }

        return Task.FromResult(0);
    }

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

    public async Task<IEnumerable<object>> GetTopGenresAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, 11, 1); // Ноябрь
        var end = start.AddMonths(1).AddDays(-1);
        var top = await _gameRepository.GetTopGenresAsync(5, start, end);

        var list = new List<object>();
        foreach (var kv in top) list.Add(new { genre = kv.Key, games = kv.Value.GamesCount, avgFollowers = kv.Value.AvgFollowers });

        return list;
    }

    public async Task<object> GetDynamicsAsync()
    {
        var dynamics = await _gameRepository.GetGenreDynamicsAsync(3);

        // 1. Группируем по жанру
        var genreGroups = dynamics
            .GroupBy(d => d.Genre)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 2. Определяем все уникальные месяцы и сортируем
        var allMonths = dynamics
            .Select(d => d.Month.ToString("yyyy-MM"))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        // 3. Строим datasets для Chart.js
        var datasets = genreGroups.Select(kv => new
        {
            label = kv.Key,
            data = allMonths.Select(month =>
            {
                var entry = kv.Value.FirstOrDefault(x => x.Month.ToString("yyyy-MM") == month);
                // проверяем по умолчанию (Month == default)
                return entry.Month != default ? Math.Round(entry.AvgFollowers, 1) : (double?)null;
            }).ToArray()
        }).ToList();

        // 4. Возвращаем готовую структуру
        return new
        {
            labels = allMonths,
            datasets = datasets
        };
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
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
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

public class SteamBackgroundService : BackgroundService
{
    private readonly ISteamService _steamService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SteamBackgroundService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6); // Синхронизация каждые 6 часов

    public SteamBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SteamBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting Steam data synchronization...");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var steamService = scope.ServiceProvider.GetRequiredService<ISteamService>();

                    var now = DateTime.UtcNow;
                    //var startDate = now.AddMonths(-1); // Игры от месяца назад
                    //var endDate = now.AddYears(1);     // Игры до года вперед
                    var startDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);  // Начало 2024
                    var endDate = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc); // Конец 2026

                    await steamService.SyncUpcomingGamesAsync(startDate, endDate);
                }

                _logger.LogInformation("Steam data synchronization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Steam data synchronization");
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }
}

// Вспомогательные классы для десериализации Steam API
public class AppListResponse
{
    public AppList applist { get; set; }
}

public class AppList
{
    public List<App> apps { get; set; }
}

public class App
{
    public int appid { get; set; }
    public string name { get; set; }
}

public class AppDetails
{
    public bool success { get; set; }
    public AppData data { get; set; }
}

public class AppData
{
    public string name { get; set; }
    public ReleaseDate release_date { get; set; }
    public string short_description { get; set; }
    public string header_image { get; set; }
    public Platforms platforms { get; set; }
    public List<Genre> genres { get; set; }
}

public class ReleaseDate
{
    public bool coming_soon { get; set; }
    public string date { get; set; }
}

public class Platforms
{
    public bool windows { get; set; }
    public bool mac { get; set; }
    public bool linux { get; set; }
}

public class Genre
{
    public string description { get; set; }
}

