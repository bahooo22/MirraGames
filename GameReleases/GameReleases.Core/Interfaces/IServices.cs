using System.Linq.Expressions;

using GameReleases.Core.DTO;
using GameReleases.Infrastructure.Entities;

namespace GameReleases.Core.Interfaces;
public interface IServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    where TEntity : class
    where TCreateRequest : class
    where TUpdateRequest : class
    where TResponse : class
{
    /// <summary>
    /// Read
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TResponse?> GetByIdAsync(TId id);
    Task<IEnumerable<TResponse>> GetAllAsync();
    Task<IEnumerable<TResponse>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// Pagination
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="predicate"></param>
    /// <param name="orderBy"></param>
    /// <param name="ascending"></param>
    /// <returns></returns>
    Task<PagedResponse<TResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true);

    /// <summary>
    /// Create
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<TResponse> CreateAsync(TCreateRequest request);

    /// <summary>
    /// Update
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    Task<TResponse?> UpdateAsync(TId id, TUpdateRequest request);

    /// <summary>
    /// Delete
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(TId id);

    /// <summary>
    /// Utility
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
}

public interface ISteamFollowersService
{
    Task<int> GetFollowersAsync(string appId, CancellationToken ct = default);
}

public interface IGameService : IServices<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>
{
    /// <summary>
    /// Специфичные методы для Game
    /// </summary>
    /// <param name="appId"></param>
    /// <returns></returns>
    Task<GameResponse?> GetByAppIdAsync(string appId);
    Task<IEnumerable<GameResponse>> GetByGenreAsync(string genre);
    Task<IEnumerable<GameResponse>> GetRecentGamesAsync(int days = 7);
    Task<IEnumerable<GameResponse>> GetPopularGamesAsync(int count = 10);
    Task<PagedResponse<GameResponse>> GetPagedWithFiltersAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? genre = null,
        string? platform = null);
    Task<bool> ExistsByAppIdAsync(string appId);

    Task<IEnumerable<GameResponse>> GetReleasesAsync(string month, string? platform = null, string? genre = null);
    Task<CalendarResponse> GetCalendarAsync(string month);

    Task<PagedResponse<GameResponse>> GetReleasesPagedAsync(
        string month,
        string? platform = null,
        string? genre = null,
        int pageNumber = 1,
        int pageSize = 20);
}

public interface IAnalyticsService
{
    Task<IEnumerable<GenreStatsResponse>> GetTopGenresAsync(string month);
    Task<GenreDynamicsResultResponse> GetDynamicsAsync(string monthsCsv);
}


public interface ISteamService
{
    Task SyncUpcomingGamesAsync(DateTime startDate, DateTime endDate); // Синхронизация данных
    Task<IEnumerable<Game>> GetReleasesAsync(string month); // e.g., "2025-11"
    Task<IEnumerable<object>> GetCalendarAsync(string month); // Агрегированный календарь
    Task<IEnumerable<object>> GetTopGenresAsync(string month); // Топ-5 жанров
    Task<object> GetDynamicsAsync(string month); // Динамика за 3 месяца
}

public interface ISteamSyncService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}

public interface IJwtService
{
    string GenerateToken(string username);
    bool ValidateToken(string token);
}