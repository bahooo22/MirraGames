using System.ComponentModel.DataAnnotations;

namespace GameReleases.Infrastructure.Entities;

public class Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string AppId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public ICollection<string> Genres { get; set; } = [];
    public int Followers { get; set; }
    [Url] public string StoreUrl { get; set; } = string.Empty;
    [Url] public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public ICollection<string> Platforms { get; set; } = [];
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class GameHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid GameId { get; init; }
    public Game Game { get; init; } = null!;
    public DateTime CollectedAt { get; init; } = DateTime.UtcNow;
    public int Followers { get; init; }
    public ICollection<string> Genres { get; init; } = [];
}