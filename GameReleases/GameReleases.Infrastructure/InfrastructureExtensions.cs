using GameReleases.Infrastructure.Data;
using GameReleases.Infrastructure.Interfaces;
using GameReleases.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GameReleases.Infrastructure;

public static class InfrastructureExtensions
{
    // Базовый метод для регистрации репозиториев
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddSpecificRepository<IGameRepository, GameRepository>();
        services.AddSpecificRepository<IAnalyticsRepository, ClickHouseAnalyticsRepository>();

        return services;
    }

    // Метод для регистрации специфичных репозиториев
    private static IServiceCollection AddSpecificRepository<TRepository, TImplementation>(this IServiceCollection services)
        where TRepository : class
        where TImplementation : class, TRepository
    {
        services.AddScoped<TRepository, TImplementation>();
        return services;
    }

    // Комплексный метод для регистрации всего сразу (DbContext + репозитории)
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

    // Метод ТОЛЬКО для миграций (без сервисов)
    public static IServiceCollection AddDbContextForMigrations(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        return services;
    }
}