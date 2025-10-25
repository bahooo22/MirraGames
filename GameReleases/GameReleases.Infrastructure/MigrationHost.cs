using GameReleases.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Этот класс используется только для миграций EF Core
public static class MigrationHost
{
    public static IServiceProvider CreateMigrationServiceProvider()
    {
        var services = new ServiceCollection();
        IConfiguration config = new ConfigurationBuilder().Build();

        // Регистрируем только DbContext без всех сервисов
        services.AddDbContextForMigrations(config.GetConnectionString("DefaultConnection")!);

        return services.BuildServiceProvider();
    }
}