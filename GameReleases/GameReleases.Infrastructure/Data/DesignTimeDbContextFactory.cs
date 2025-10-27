using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GameReleases.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1️ Пытаемся взять из аргумента --connection
        string? cliConnection = null;
        if (args != null && args.Length > 0)
        {
            var index = Array.IndexOf(args, "--connection");
            if (index >= 0 && index < args.Length - 1)
                cliConnection = args[index + 1];
        }

        // 2️ Из ENV переменной (docker-compose передаёт её)
        var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        // 3️ Из конфигов
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddJsonFile("appsettings.Production.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var jsonConnection = config.GetConnectionString("DefaultConnection");

        // 4️ Выбираем приоритет — CLI → ENV → JSON
        var connectionString = cliConnection ?? envConnection ?? jsonConnection;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("No connection string found for 'DefaultConnection'.");

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString,
            opt => opt.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.FullName));

        Console.WriteLine($"[DesignTimeDbContextFactory] Using connection: {connectionString}");
        return new AppDbContext(optionsBuilder.Options);
    }
}
