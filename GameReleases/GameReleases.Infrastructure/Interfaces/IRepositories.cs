using System.Linq.Expressions;

using GameReleases.Infrastructure.DTO;
using GameReleases.Infrastructure.Entities;

namespace GameReleases.Infrastructure.Interfaces;

public interface IRepository<TEntity> where TEntity : class
{
    // Read
    Task<TEntity?> GetByIdAsync(Guid id);
    Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includes);

    // Pagination
    Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true);

    // Create
    Task<TEntity> AddAsync(TEntity entity);
    Task<IEnumerable<TEntity>> AddRangeAsync(IEnumerable<TEntity> entities);

    // Update
    Task<TEntity> UpdateAsync(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);

    // Delete
    void Remove(TEntity entity);
    void RemoveRange(IEnumerable<TEntity> entities);

    // Utility
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    Task SaveChangesAsync();
}

public interface IGameRepository : IRepository<Game>
{
    Task<Game?> GetByAppIdAsync(ulong appId);
    Task<IEnumerable<Game>> GetByGenreAsync(string genre);
    Task<IEnumerable<Game>> GetRecentGamesAsync(int days = 7);
    Task<IEnumerable<Game>> GetPopularGamesAsync(int count = 10);
    Task<IEnumerable<Game>> GetUpcomingGamesAsync(DateTime startDate,
        DateTime endDate,
        string? platform = null,
        string? genre = null);
    Task<IEnumerable<Game>> GetGamesByMonthAsync(int year, int month);
    Task<IEnumerable<Game>> GetGamesByDateRangeAsync(DateTime startDate, DateTime endDate);
}

public interface IAnalyticsRepository
{
    /// <summary>
    /// Сохраняет снапшот игры в ClickHouse (для последующего анализа динамики).
    /// </summary>
    Task StoreGameAnalyticsAsync(Game game);

    /// <summary>
    /// Возвращает агрегированную статистику по жанрам за указанный период.
    /// </summary>
    Task<List<GenreAnalytics>> GetGenreAnalyticsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Возвращает количество релизов по дням в заданном диапазоне.
    /// </summary>
    Task<List<DailyReleaseCount>> GetDailyReleasesAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Возвращает динамику по жанрам за несколько месяцев.
    /// </summary>
    Task<List<GenreDynamics>> GetGenreDynamicsAsync(IEnumerable<string> months);
}


