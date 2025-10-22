using System.Linq.Expressions;

using GameReleases.Infrastructure.Data;
using GameReleases.Infrastructure.DTO;
using GameReleases.Infrastructure.Entities;
using GameReleases.Infrastructure.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace GameReleases.Infrastructure.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    // Read
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

    // Pagination
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

    // Create
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

    // Update
    public virtual TEntity Update(TEntity entity)
    {
        var entry = _dbSet.Update(entity);
        return entry.Entity;
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    // Delete
    public virtual void Remove(TEntity entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    // Utility
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

    public async Task UpdateAsync(Game game)
    {
        context.Games.Update(game);
        await context.SaveChangesAsync();
    }

    public async Task<Game?> GetByAppIdAsync(string appId, DateTime? collectedAt = null)
    {
        collectedAt ??= DateTime.UtcNow.Date; // По умолчанию сегодняшний
        return await context.Games.FirstOrDefaultAsync(g => g.AppId == appId && g.CollectedAt.Date == collectedAt.Value.Date);
    }

    public async Task<IEnumerable<Game>> GetUpcomingGamesAsync(DateTime startDate, DateTime endDate, string platform = null, string genre = null)
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

    public async Task<IDictionary<string, (int GamesCount, double AvgFollowers)>> GetTopGenresAsync(int topCount, DateTime startDate, DateTime endDate)
    {
        var utcStartDate = startDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(startDate, DateTimeKind.Utc)
            : startDate.ToUniversalTime();

        var utcEndDate = endDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(endDate, DateTimeKind.Utc)
            : endDate.ToUniversalTime();

        var games = await _dbSet
            .Where(g => g.ReleaseDate >= utcStartDate && g.ReleaseDate <= utcEndDate)
            .ToListAsync();

        var genreStats = games
            .SelectMany(g => g.Genres)
            .GroupBy(genre => genre)
            .Select(group => new
            {
                Genre = group.Key,
                GamesCount = group.Count(),
                AvgFollowers = games.Where(g => g.Genres.Contains(group.Key)).Average(g => g.Followers)
            })
            .OrderByDescending(s => s.GamesCount)
            .Take(topCount)
            .ToDictionary(s => s.Genre, s => (s.GamesCount, s.AvgFollowers));

        return genreStats;
    }

    public async Task<IEnumerable<(string Genre, DateTime Month, int GamesCount, double AvgFollowers)>> GetGenreDynamicsAsync(int monthsBack)
    {
        var now = DateTime.UtcNow;
        var start = now.AddMonths(-monthsBack + 1).Date; // e.g., for 3 months: sept-oct-nov

        var games = await context.Games
            .Where(g => g.CollectedAt >= start)
            .ToListAsync();

        var dynamics = games
            .GroupBy(g => new { Month = new DateTime(g.CollectedAt.Year, g.CollectedAt.Month, 1) })
            .SelectMany(monthGroup => monthGroup.SelectMany(g => g.Genres).GroupBy(genre => genre)
                .Select(genreGroup => (Genre: genreGroup.Key, Month: monthGroup.Key.Month,
                    GamesCount: genreGroup.Count(),
                    AvgFollowers: monthGroup.Where(g => g.Genres.Contains(genreGroup.Key)).Average(g => g.Followers))))
            .ToHashSet();

        return dynamics;
    }

    public async Task<IEnumerable<Game>> GetGamesByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(g => g.ReleaseDate >= startDate && g.ReleaseDate <= endDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<DailyReleaseCount>> GetDailyReleaseCountsAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await _dbSet
            .Where(g => g.ReleaseDate >= startDate && g.ReleaseDate <= endDate)
            .Where(g => g.ReleaseDate.HasValue) // Добавляем проверку на null
            .GroupBy(g => g.ReleaseDate!.Value.Date) // Используем ! для уверенности что не null
            .Select(g => new DailyReleaseCount(g.Key, g.Count()))
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<IEnumerable<GenreStats>> GetGenreStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        var games = await _dbSet
            .Where(g => g.ReleaseDate >= startDate && g.ReleaseDate <= endDate)
            .ToListAsync();

        return games
            .SelectMany(g => g.Genres.Select(genre => new { Genre = genre, Game = g }))
            .GroupBy(x => x.Genre)
            .Select(g => new GenreStats(
                g.Key,
                g.Count(),
                g.Average(x => x.Game.Followers)
            ))
            .OrderByDescending(g => g.GamesCount)
            .Take(5)
            .ToList();
    }

    public async Task<IEnumerable<GenreDynamics>> GetGenreDynamicsByMonthAsync(int monthsBack = 3)
    {
        var endDate = DateTime.UtcNow;
        var startDate = endDate.AddMonths(-monthsBack);

        var monthlyData = await _dbSet
            .Where(g => g.CollectedAt >= startDate)
            .ToListAsync();

        return monthlyData
            .GroupBy(g => new
            {
                Genre = g.Genres.FirstOrDefault(), // Берем первый жанр для упрощения
                Month = new DateTime(g.CollectedAt.Year, g.CollectedAt.Month, 1)
            })
            .Select(g => new GenreDynamics(
                g.Key.Genre ?? "Unknown",
                g.Key.Month.ToString("yyyy-MM"),
                g.Count(),
                g.Average(x => x.Followers)
            ))
            .OrderBy(g => g.Month)
            .ThenByDescending(g => g.GamesCount)
            .ToList();
    }
}