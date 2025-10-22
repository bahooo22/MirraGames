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
    TEntity Update(TEntity entity);
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
    Task<Game?> GetByAppIdAsync(string appId);
    Task<IEnumerable<Game>> GetByGenreAsync(string genre);
    Task<IEnumerable<Game>> GetRecentGamesAsync(int days = 7);
    Task<IEnumerable<Game>> GetPopularGamesAsync(int count = 10);
    new Task<Game> AddAsync(Game game);
    Task UpdateAsync(Game game);
    Task<IEnumerable<Game>> GetUpcomingGamesAsync(DateTime startDate, DateTime endDate, string platform = null, string genre = null);
    Task<IEnumerable<Game>> GetGamesByMonthAsync(int year, int month);
    Task<IDictionary<string, (int GamesCount, double AvgFollowers)>> GetTopGenresAsync(int topCount, DateTime startDate, DateTime endDate);
    Task<IEnumerable<(string Genre, DateTime Month, int GamesCount, double AvgFollowers)>> GetGenreDynamicsAsync(int monthsBack);

    // Новые методы для аналитики
    Task<IEnumerable<Game>> GetGamesByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<DailyReleaseCount>> GetDailyReleaseCountsAsync(int year, int month);
    Task<IEnumerable<GenreStats>> GetGenreStatisticsAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<GenreDynamics>> GetGenreDynamicsByMonthAsync(int monthsBack = 3);
}

public interface IAnalyticsRepository
{
    Task StoreGameAnalyticsAsync(Game game);
    Task<List<GenreAnalytics>> GetGenreAnalyticsAsync(DateTime startDate, DateTime endDate);
}

public record GenreAnalytics(string Genre, int GameCount, double AvgFollowers, DateTime Period);
