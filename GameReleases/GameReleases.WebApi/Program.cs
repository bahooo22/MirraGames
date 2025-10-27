using GameReleases.Core;
using GameReleases.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Приоритет ENV над appsettings.json
var envConn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
if (!string.IsNullOrWhiteSpace(envConn))
    builder.Configuration["ConnectionStrings:DefaultConnection"] = envConn;

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "GameReleases API", 
        Version = "v1",
        Description = "API for managing game releases and Steam synchronization"
    });
});

// Регистрация слоя доступа к данным
builder.Services.AddDataAccess(
    builder.Configuration.GetConnectionString("DefaultConnection")!);

// Регистрация сервисов приложения
builder.Services.AddApplicationServices();

// JWT Authentication
builder.Services.AddJwtServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
var enableSwagger = app.Environment.IsDevelopment()
    || string.Equals(builder.Configuration["ENABLE_SWAGGER"], "true", StringComparison.OrdinalIgnoreCase);

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GameReleases API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
    });

    if (app.Environment.IsDevelopment())
        app.MapGet("/", () => Results.Redirect("/swagger"));
    else
        app.MapGet("/", () => Results.Redirect("/swagger/index.html"));
}


// app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();