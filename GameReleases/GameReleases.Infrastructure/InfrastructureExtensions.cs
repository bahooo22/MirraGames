using GameReleases.Infrastructure.Data;
using GameReleases.Infrastructure.Interfaces;
using GameReleases.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameReleases.Infrastructure;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Базовый метод для регистрации репозиториев
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddSpecificRepository<IGameRepository, GameRepository>();
        services.AddSpecificRepository<IAnalyticsRepository, ClickHouseAnalyticsRepository>();

        return services;
    }

    /// <summary>
    /// Метод для регистрации специфичных репозиториев
    /// </summary>
    /// <typeparam name="TRepository"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IServiceCollection AddSpecificRepository<TRepository, TImplementation>(this IServiceCollection services)
        where TRepository : class
        where TImplementation : class, TRepository
    {
        services.AddScoped<TRepository, TImplementation>();
        return services;
    }

    /// <summary>
    /// Комплексный метод для регистрации всего сразу (DbContext + репозитории)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <param name="dbContextOptions"></param>
    /// <returns></returns>
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        string connectionString,
        Action<DbContextOptionsBuilder>? dbContextOptions = null)
    {
        // Регистрация DbContext
        if (dbContextOptions != null)
        {
            services.AddDbContext<AppDbContext>(dbContextOptions);
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString,
                    x => x.MigrationsAssembly("GameReleases.Infrastructure")));
        }

        // Регистрация репозиториев
        services.AddRepositories();

        return services;
    }

    /// <summary>
    /// Метод ТОЛЬКО для миграций (без сервисов)
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <returns></returns>
    public static IServiceCollection AddDbContextForMigrations(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        return services;
    }
}