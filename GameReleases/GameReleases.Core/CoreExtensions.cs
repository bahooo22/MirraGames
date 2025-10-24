using System.Text;

using GameReleases.Core.Interfaces;
using GameReleases.Core.Models;
using GameReleases.Core.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GameReleases.Core;

public static class CoreExtensions
{
    // Регистрация обобщённого CRUD сервиса
    public static IServiceCollection AddCrudServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>(
        this IServiceCollection services)
        where TEntity : class
        where TCreateRequest : class
        where TUpdateRequest : class
        where TResponse : class
    {
        // Регистрируем обобщённый интерфейс сервиса с его реализацией
        services.TryAddScoped(
            typeof(IServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>),
            typeof(Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>));

        return services;
    }

    // Регистрация конкретного сервиса с его интерфейсом
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        // Регистрируем конкретный сервис
        services.TryAddScoped<IGameService, GameService>();

        return services;
    }

    // Массовая регистрация всех сервисов приложения
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Регистрируем обобщённые CRUD сервисы для конкретных моделей
        //services.AddCrudServices<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>();

        // Регистрируем конкретные сервисы
        services.AddGameServices();

        // Регистрируем сервисы для работы cо Steam
        services.AddSteamServices();

        // Аналитический сервис
        services.AddScoped<IAnalyticsService, AnalyticsService>();


        return services;
    }

    // Универсальный метод для регистрации любого конкретного сервиса
    public static IServiceCollection AddCustomService<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.TryAddScoped<TService, TImplementation>();
        return services;
    }

    // Метод для регистрации сервисов с конфигурацией
    public static IServiceCollection AddConfiguredServices(this IServiceCollection services, Action<ServiceConfiguration> configure)
    {
        var config = new ServiceConfiguration();
        configure?.Invoke(config);

        // Здесь можно добавить дополнительную логику на основе конфигурации
        if (config.EnableLogging)
        {
            // Дополнительная регистрация для логирования
        }

        return services.AddApplicationServices();
    }

    public static IServiceCollection AddJwtServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Конфигурация JWT
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.AddScoped<IJwtService, JwtService>();

        // Аутентификация
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JwtSettings:Issuer"],
                    ValidAudience = configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!))
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSteamServices(this IServiceCollection services)
    {
        services.TryAddTransient<ISteamService, SteamService>();
        services.AddHostedService<SteamBackgroundService>();
        services.TryAddSingleton<ISteamFollowersService, SteamFollowersService>();

        return services;
    }
}

// Класс для конфигурации сервисов
public class ServiceConfiguration
{
    public bool EnableLogging { get; set; } = true;
    public bool EnableCaching { get; set; } = false;
}