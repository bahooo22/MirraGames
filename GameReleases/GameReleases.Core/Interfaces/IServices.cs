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
    // Read
    Task<TResponse?> GetByIdAsync(TId id);
    Task<IEnumerable<TResponse>> GetAllAsync();
    Task<IEnumerable<TResponse>> FindAsync(Expression<Func<TEntity, bool>> predicate);

    // Pagination
    Task<PagedResponse<TResponse>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool ascending = true);

    // Create
    Task<TResponse> CreateAsync(TCreateRequest request);

    // Update
    Task<TResponse?> UpdateAsync(TId id, TUpdateRequest request);

    // Delete
    Task<bool> DeleteAsync(TId id);

    // Utility
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
}

public interface IGameService : IServices<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>
{
    // Специфичные методы для Game
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
}

public interface ISteamService
{
    Task SyncUpcomingGamesAsync(DateTime startDate, DateTime endDate); // Синхронизация данных
    Task<IEnumerable<Game>> GetReleasesAsync(string month); // e.g., "2025-11"
    Task<IEnumerable<object>> GetCalendarAsync(string month); // Агрегированный календарь
    Task<IEnumerable<object>> GetTopGenresAsync(); // Топ-5 жанров
    Task<object> GetDynamicsAsync(); // Динамика за 3 месяца
}

public interface IJwtService
{
    string GenerateToken(string username);
    bool ValidateToken(string token);
}