using GameReleases.Infrastructure;
using GameReleases.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Этот класс используется только для миграций EF Core
public static class MigrationHost
{
    public static IServiceProvider CreateMigrationServiceProvider()
    {
        var services = new ServiceCollection();

        // Регистрируем только DbContext без всех сервисов
        services.AddDbContextForMigrations(
            "Host=localhost;Port=5432;Database=game_releases;Username=postgres;Password=password");

        return services.BuildServiceProvider();
    }
}