using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GameReleases.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Используем строку подключения для разработки
        // TODO: или брать из appsettings.json
        const string connectionString = "Host=localhost;Port=5432;Database=game_releases;Username=postgres;Password=1234";

        optionsBuilder.UseNpgsql(connectionString,
            x => x.MigrationsAssembly("GameReleases.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }
}