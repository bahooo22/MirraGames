using GameReleases.Core;
using GameReleases.Core.Interfaces;
using GameReleases.Core.Services;
using GameReleases.Infrastructure;
using GameReleases.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрация слоя доступа к данным
builder.Services.AddDataAccess(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

// Регистрация сервисов приложения
builder.Services.AddApplicationServices();

// JWT Authentication
builder.Services.AddJwtServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
else
{
    // В Production используем более безопасный подход
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Проверяем есть ли pending миграции
        var pendingMigrations = context.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            // В продакшене лучше логировать и применять миграции через CI/CD
            Console.WriteLine($"Found {pendingMigrations.Count()} pending migrations:");
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($" - {migration}");
            }

            // Только если явно разрешено в конфигурации
            if (builder.Configuration.GetValue<bool>("ApplyMigrationsInProduction"))
            {
                context.Database.Migrate();
            }
        }
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();