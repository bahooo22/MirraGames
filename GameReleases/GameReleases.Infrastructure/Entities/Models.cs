using System.ComponentModel.DataAnnotations;

namespace GameReleases.Infrastructure.Entities;

public class Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AppId { get; init; } = string.Empty;// Steam AppID (уникальный)
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public HashSet<string> Genres { get; set; } = []; // Жанры/теги как список строк
    public int Followers { get; set; } // Количество фолловеров/wishlists
    [Url]
    public string StoreUrl { get; set; } = string.Empty;

    [Url]
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public HashSet<string> Platforms { get; set; } = []; // e.g., ["Windows", "Mac", "Linux"]

    // Для динамики: дата сбора данных
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class GameHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid GameId { get; init; } = Guid.Empty;
    public Game Game { get; init; } = null!;
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
    public int Followers { get; init; }
    public HashSet<string> Genres { get; init; } = [];
}

public class GameAnalytics
{
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public string Genre { get; set; } = string.Empty;
    public int GamesCount { get; set; }
    public double AvgFollowers { get; set; }
    public string Month { get; set; } = string.Empty;
}