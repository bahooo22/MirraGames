using System.Reflection;
using System.Text;

using GameReleases.Core.DTO;
using GameReleases.Core.Interfaces;
using GameReleases.Core.Models;
using GameReleases.Core.Services;
using GameReleases.Infrastructure.Entities;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace GameReleases.Core;

public static class CoreExtensions
{
    /// <summary>
    /// Регистрация обобщённого CRUD сервиса с правильными ограничениями
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TId"></typeparam>
    /// <typeparam name="TCreateRequest"></typeparam>
    /// <typeparam name="TUpdateRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCrudServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>(
        this IServiceCollection services)
        where TEntity : class
        where TId : notnull
        where TCreateRequest : class
        where TUpdateRequest : class
        where TResponse : class
    {
        // Регистрируем обобщённый интерфейс сервиса с его реализацией
        // services.TryAddScoped(
            // typeof(IServices<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>),
            // typeof(Services<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>));

        return services;
    }

    /// <summary>
    /// Регистрация конкретного сервиса с его интерфейсом
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
        // Регистрируем конкретный сервис
        services.TryAddScoped<IGameService, GameService>();

        // Регистрируем обобщенный CRUD сервис для Game
        services.AddCrudServices<Game, Guid, CreateGameRequest, UpdateGameRequest, GameResponse>();

        return services;
    }

    /// <summary>
    /// Массовая регистрация всех сервисов приложения
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Регистрируем обобщённые CRUD сервисы для конкретных моделей
        // Раскомментируйте при необходимости для других сущностей:
        // services.AddCrudServices<AnotherEntity, Guid, CreateAnotherRequest, UpdateAnotherRequest, AnotherResponse>();

        // Регистрируем конкретные сервисы
        services.AddGameServices();

        // Регистрируем сервисы для работы cо Steam
        services.AddSteamServices();

        // Аналитический сервис
        services.AddScoped<IAnalyticsService, AnalyticsService>();

        services.AddScoped<ISteamSyncService, SteamSyncService>();

        return services;
    }

    /// <summary>
    /// Универсальный метод для регистрации любого конкретного сервиса
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    /// <typeparam name="TImplementation"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCustomService<TService, TImplementation>(this IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        services.TryAddScoped<TService, TImplementation>();
        return services;
    }

    /// <summary>
    /// Метод для автоматической регистрации всех сервисов, реализующих определенный интерфейс
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assembly"></param>
    /// <param name="interfaceFilter"></param>
    /// <param name="implementationFilter"></param>
    /// <returns></returns>
    public static IServiceCollection AddScopedByConvention(this IServiceCollection services,
        Assembly assembly,
        Func<Type, bool> interfaceFilter,
        Func<Type, bool> implementationFilter)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && implementationFilter(t))
            .ToList();

        foreach (var implementation in implementations)
        {
            var interfaces = implementation.GetInterfaces()
                .Where(interfaceFilter)
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.TryAddScoped(interfaceType, implementation);
            }
        }

        return services;
    }

    /// <summary>
    /// Метод для регистрации сервисов с конфигурацией
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
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

    private static IServiceCollection AddSteamServices(this IServiceCollection services)
    {
        services.TryAddTransient<ISteamService, SteamService>();
        services.AddHostedService<SteamBackgroundService>();
        services.TryAddSingleton<ISteamFollowersService, SteamFollowersService>();

        return services;
    }
}

/// <summary>
/// Вспомогательный класс для конфигурации
/// </summary>
public class ServiceConfiguration
{
    public bool EnableLogging { get; set; } = true;
    public bool EnableCaching { get; set; } = false;
    public bool EnableValidation { get; set; } = true;
}