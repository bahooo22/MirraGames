using GameReleases.Core;
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
builder.Services.AddSingleton<ClickHouseContext>(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("ClickHouseConnection")
                  ?? throw new InvalidOperationException("ClickHouse connection string not found");
    return new ClickHouseContext(connStr);
});

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();