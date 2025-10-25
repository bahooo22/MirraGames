using System.Linq.Expressions;

using ClickHouse.Client.ADO;
using ClickHouse.Client.ADO.Parameters;

using GameReleases.Infrastructure.Data;
using GameReleases.Infrastructure.DTO;
using GameReleases.Infrastructure.Entities;
using GameReleases.Infrastructure.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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

    public ClickHouseAnalyticsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ClickHouseConnection")
                            ?? throw new InvalidOperationException("ClickHouse connection string not configured");
    }

    /// <summary>
    /// Сохраняет снапшот игры в ClickHouse (по жанрам).
    /// </summary>
    public async Task StoreGameAnalyticsAsync(Game game)
    {
        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"INSERT INTO game_analytics 
                    (CollectedAt, Genre, GamesCount, AvgFollowers, Month)
                    VALUES (@collectedAt, @genre, @gamesCount, @avgFollowers, @month)";

        foreach (var genre in game.Genres)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "collectedAt", Value = game.CollectedAt });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "genre", Value = genre });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "gamesCount", Value = 1 });
            cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "avgFollowers", Value = game.Followers });
            cmd.Parameters.Add(new ClickHouseDbParameter
            { ParameterName = "month", Value = game.ReleaseDate?.ToString("yyyy-MM") ?? "" });

            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Возвращает агрегированную статистику по жанрам за период.
    /// </summary>
    public async Task<List<GenreAnalytics>> GetGenreAnalyticsAsync(DateTime startDate, DateTime endDate)
    {
        var result = new List<GenreAnalytics>();

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT Genre,
                           count() AS GameCount,
                           avg(AvgFollowers) AS AvgFollowers,
                           toStartOfMonth(CollectedAt) AS Period
                    FROM game_analytics
                    WHERE CollectedAt BETWEEN @start AND @end
                    GROUP BY Genre, Period
                    ORDER BY Period, GameCount DESC";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "start", Value = startDate });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "end", Value = endDate });

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new GenreAnalytics
            {
                Genre = reader.GetString(0),
                GameCount = reader.GetInt32(1),
                AvgFollowers = reader.GetDouble(2),
                Period = reader.GetDateTime(3)
            });
        }

        return result;
    }

    /// <summary>
    /// Возвращает количество релизов по дням.
    /// </summary>
    public async Task<List<DailyReleaseCount>> GetDailyReleasesAsync(DateTime startDate, DateTime endDate)
    {
        var result = new List<DailyReleaseCount>();

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT toDate(ReleaseDate) AS Date, count() AS Count
                    FROM games
                    WHERE ReleaseDate BETWEEN @start AND @end
                    GROUP BY Date
                    ORDER BY Date";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "start", Value = startDate });
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "end", Value = endDate });

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new DailyReleaseCount
            {
                Date = reader.GetDateTime(0),
                Count = reader.GetInt32(1)
            });
        }

        return result;
    }

    /// <summary>
    /// Возвращает динамику по жанрам за несколько месяцев.
    /// </summary>
    public async Task<List<GenreDynamics>> GetGenreDynamicsAsync(IEnumerable<string> months)
    {
        var result = new List<GenreDynamics>();

        await using var conn = new ClickHouseConnection(_connectionString);
        await conn.OpenAsync();

        var sql = @"SELECT arrayJoin(Genres) AS Genre,
                           formatDateTime(ReleaseDate, '%Y-%m') AS Month,
                           count() AS GamesCount,
                           avg(Followers) AS AvgFollowers
                    FROM games
                    WHERE formatDateTime(ReleaseDate, '%Y-%m') IN @months
                    GROUP BY Genre, Month
                    ORDER BY Month, GamesCount DESC";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.Add(new ClickHouseDbParameter { ParameterName = "months", Value = months.ToArray() });

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new GenreDynamics
            {
                Genre = reader.GetString(0),
                Month = reader.GetString(1),
                GamesCount = reader.GetInt32(2),
                AvgFollowers = reader.GetDouble(3)
            });
        }

        return result;
    }


}

public class GameRepository(AppDbContext context) : Repository<Game>(context), IGameRepository
{
    public async Task<Game?> GetByAppIdAsync(string appId)
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
