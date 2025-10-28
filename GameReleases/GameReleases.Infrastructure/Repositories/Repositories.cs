using System.Data;
using System.Linq.Expressions;

using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;

using GameReleases.Infrastructure.Data;
using GameReleases.Infrastructure.DTO;
using GameReleases.Infrastructure.Entities;
using GameReleases.Infrastructure.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameReleases.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity>
    where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Read
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.Where(predicate).ToListAsync();
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
    public virtual async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true)
    {
        IQueryable<TEntity> query = _dbSet;

        // Apply filter if provided
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering
        if (orderBy != null)
        {
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
        }

        // Apply pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Create
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities)
    {
        var entityList = entities.ToList();
        await _dbSet.AddRangeAsync(entityList);
        return entityList;
    }

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    /// <summary>
    /// Delete
    /// </summary>
    /// <param name="entity"></param>
    public virtual void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    /// <summary>
    /// Utility
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public virtual async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

public class ClickHouseAnalyticsRepository : IAnalyticsRepository
{
    private readonly string _connectionString;
    private readonly ILogger<ClickHouseAnalyticsRepository> _logger;

    public ClickHouseAnalyticsRepository(IConfiguration configuration, ILogger<ClickHouseAnalyticsRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("ClickHouseConnection")
                            ?? throw new InvalidOperationException("ClickHouse connection string not configured");
        _logger = logger;
    }

    // =====================================================
    // STORE GAME ANALYTICS
    // =====================================================
    /// <summary>
    /// Сохраняет информацию об игре и её жанрах в ClickHouse.
    /// </summary>
    public async Task StoreGameAnalyticsAsync(Game game)
    {
        if (game == null || game.Genres == null || game.Genres.Count == 0)
        {
            _logger?.LogWarning("⚠️ Skipping game analytics storage: invalid game or empty genres.");
            return;
        }

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();
        await EnsureClickHouseReadyAsync(conn);

        const string sql = @"
            INSERT INTO game_analytics.games 
            (AppId, Name, ReleaseDate, Genres, Followers, Description, Platforms, StoreUrl, PosterUrl, CollectedAt)
            VALUES (@appId, @name, @releaseDate, @genres, @followers, @desc, @platforms, @storeUrl, @posterUrl, @collectedAt)";

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "appId", Value = game.AppId });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "name", Value = game.Name ?? "" });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "releaseDate", Value = game.ReleaseDate ?? DateTime.UtcNow });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "genres", Value = game.Genres.ToArray() });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "followers", Value = (ulong)game.Followers });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "desc", Value = game.ShortDescription ?? "" });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "platforms", Value = game.Platforms?.ToArray() ?? Array.Empty<string>() });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "storeUrl", Value = game.StoreUrl ?? "" });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "posterUrl", Value = game.PosterUrl ?? "" });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "collectedAt", Value = game.CollectedAt });

            await cmd.ExecuteNonQueryAsync();
            _logger?.LogInformation("✅ Stored analytics for game '{Name}' ({AppId}) with {Genres} genres.", game.Name, game.AppId, game.Genres.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Failed to store analytics for game {AppId}", game.AppId);
            throw;
        }
    }

    // =====================================================
    // GET GENRE ANALYTICS
    // =====================================================
    /// <summary>
    /// Возвращает агрегированную статистику по жанрам за период.
    /// </summary>
    public async Task<List<GenreAnalytics>> GetGenreAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var result = new List<GenreAnalytics>();

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();
        _logger?.LogInformation("📡 Connected to ClickHouse for genre analytics: {Start} - {End}", startDate, endDate);

        await EnsureClickHouseReadyAsync(conn);

        var sql = @"
            SELECT 
                arrayJoin(Genres) AS Genre,
                count() AS GameCount,
                avg(Followers) AS AvgFollowers,
                toStartOfMonth(CollectedAt) AS Period
            FROM game_analytics.games
            WHERE CollectedAt BETWEEN @start AND @end
            GROUP BY Genre, Period
            ORDER BY Period ASC, GameCount DESC
            LIMIT 50";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "start", Value = startDate });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "end", Value = endDate });

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new GenreAnalytics
                {
                    Genre = reader.GetString(0),
                    GameCount = Convert.ToInt32(reader.GetValue(1)),
                    AvgFollowers = reader.GetDouble(2),
                    Period = reader.GetDateTime(3)
                });
            }

            _logger?.LogInformation("✅ Retrieved {Count} genre analytics records from ClickHouse.", result.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error reading genre analytics from ClickHouse.");
            throw;
        }

        return result;
    }

    // =====================================================
    // GET DAILY RELEASES
    // =====================================================
    /// <summary>
    /// Возвращает количество релизов по дням.
    /// </summary>
    public async Task<List<DailyReleaseCount>> GetDailyReleasesAsync(DateTime startDate, DateTime endDate)
    {
        var result = new List<DailyReleaseCount>();

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();
        _logger?.LogInformation("📡 Connected to ClickHouse for daily releases: {Start} - {End}", startDate, endDate);

        await EnsureClickHouseReadyAsync(conn);

        var sql = @"
            SELECT toDate(ReleaseDate) AS Date, count() AS Count
            FROM game_analytics.games
            WHERE ReleaseDate BETWEEN @start AND @end
            GROUP BY Date
            ORDER BY Date";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "start", Value = startDate });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "end", Value = endDate });

        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DailyReleaseCount
                {
                    Date = reader.GetDateTime(0),
                    Count = Convert.ToInt32(reader.GetValue(1))
                });
            }

            _logger?.LogInformation("✅ Retrieved {Count} daily release records from ClickHouse.", result.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error reading daily releases from ClickHouse.");
            throw;
        }

        return result;
    }

    // =====================================================
    // GET GENRE DYNAMICS
    // =====================================================
    /// <summary>
    /// Возвращает динамику по жанрам за несколько месяцев.
    /// </summary>
    public async Task<List<GenreDynamics>> GetGenreDynamicsAsync(IEnumerable<string> months)
    {
        var result = new List<GenreDynamics>();
        var monthsList = months?.ToList() ?? [];

        if (monthsList.Count == 0)
            throw new ArgumentException("Parameter 'months' must contain at least one month (e.g. ['2025-11']).");

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();
        _logger?.LogInformation("📡 Connected to ClickHouse for genre dynamics (months: {Months})", string.Join(", ", monthsList));

        await EnsureClickHouseReadyAsync(conn);

        var monthsCsv = string.Join(",", monthsList.Select(m => $"'{m}'"));

        var sql = $@"
            SELECT arrayJoin(Genres) AS Genre,
                   formatDateTime(ReleaseDate, '%Y-%m') AS Month,
                   count() AS GamesCount,
                   avg(Followers) AS AvgFollowers
            FROM game_analytics.games
            WHERE formatDateTime(ReleaseDate, '%Y-%m') IN ({monthsCsv})
            GROUP BY Genre, Month
            ORDER BY Month, GamesCount DESC";

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new GenreDynamics
                {
                    Genre = reader.GetString(0),
                    Month = reader.GetString(1),
                    GamesCount = Convert.ToInt32(reader.GetValue(2)),
                    AvgFollowers = reader.GetDouble(3)
                });
            }

            _logger?.LogInformation("✅ Retrieved {Count} genre dynamics records from ClickHouse.", result.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Error reading genre dynamics from ClickHouse.");
            throw;
        }

        return result;
    }

    /// <summary>
    /// ENSURE DATABASE AND TABLES
    /// </summary>
    /// <param name="conn"></param>
    /// <returns></returns>
    private async Task EnsureClickHouseReadyAsync(ClickHouseConnection conn)
    {
        try
        {
            // Создание базы, если нет
            await using (var dbCmd = conn.CreateCommand())
            {
                dbCmd.CommandText = "CREATE DATABASE IF NOT EXISTS game_analytics";
                await dbCmd.ExecuteNonQueryAsync();
                _logger?.LogInformation("✅ ClickHouse database 'game_analytics' checked/created.");
            }

            // Создание таблицы, если нет
            await using (var tableCmd = conn.CreateCommand())
            {
                tableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS game_analytics.games
                    (
                        AppId UInt64,
                        Name String,
                        ReleaseDate DateTime,
                        Genres Array(String),
                        Followers UInt64,
                        Description String,
                        Platforms Array(String),
                        StoreUrl String,
                        PosterUrl String,
                        CollectedAt DateTime DEFAULT now()
                    )
                    ENGINE = MergeTree()
                    PARTITION BY toYYYYMM(CollectedAt)
                    ORDER BY (AppId, CollectedAt)";
                await tableCmd.ExecuteNonQueryAsync();
                _logger?.LogInformation("✅ ClickHouse table 'game_analytics.games' checked/created.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Failed to ensure ClickHouse database or table.");
            throw;
        }
    }
}

public class GameRepository(AppDbContext context) : Repository<Game>(context), IGameRepository
{
    public async Task<Game?> GetByAppIdAsync(long appId)
    {
        return await _dbSet.FirstOrDefaultAsync(g => g.AppId == appId);
    }

    public async Task<IEnumerable<Game>> GetByGenreAsync(string genre)
    {
        return await _dbSet
            .Where(g => g.Genres.Contains(genre))
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetRecentGamesAsync(int days = 7)
    {
        var dateThreshold = DateTime.UtcNow.AddDays(-days);
        return await _dbSet
            .Where(g => g.CollectedAt >= dateThreshold)
            .OrderByDescending(g => g.CollectedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetPopularGamesAsync(int count = 10)
    {
        return await _dbSet
            .OrderByDescending(g => g.Followers)
            .Take(count)
            .ToListAsync();
    }

    public override async Task<Game> AddAsync(Game game)
    {
        context.Games.Add(game);
        await context.SaveChangesAsync();
        return game;
    }

    public override async Task<Game> UpdateAsync(Game game)
    {
        context.Games.Update(game);
        await context.SaveChangesAsync();
        return game;
    }

    public async Task<IEnumerable<Game>> GetUpcomingGamesAsync(DateTime startDate, DateTime endDate,
        string? platform = null, string? genre = null)
    {
        var utcStartDate = startDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc)
            : startDate.ToUniversalTime();

        var utcEndDate = endDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc)
            : endDate.ToUniversalTime();

        var query = _dbSet.Where(g => g.ReleaseDate >= utcStartDate && g.ReleaseDate <= utcEndDate);

        if (!string.IsNullOrEmpty(platform))
            query = query.Where(g => g.Platforms.Contains(platform));

        if (!string.IsNullOrEmpty(genre))
            query = query.Where(g => g.Genres.Contains(genre));

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Game>> GetGamesByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddDays(-1);
        return await GetUpcomingGamesAsync(start, end);
    }

    public async Task<IEnumerable<Game>> GetGamesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(g => g.ReleaseDate >= startDate && g.ReleaseDate <= endDate)
            .ToListAsync();
    }
}
