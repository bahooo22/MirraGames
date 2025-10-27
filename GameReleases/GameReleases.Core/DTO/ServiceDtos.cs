using System.ComponentModel.DataAnnotations;

using GameReleases.Infrastructure.Entities;

namespace GameReleases.Core.DTO;

/// <summary>
/// Request DTOs
/// </summary>

public class CreateGameRequest
{
    [Required]
    public ulong AppId { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    [MinLength(1, ErrorMessage = "At least one genre is required")]
    public ICollection<string> Genres { get; set; } = [];

    [Range(0, ulong.MaxValue, ErrorMessage = "Followers must be non-negative")]
    public ulong Followers { get; set; }

    [Required, Url]
    public string StoreUrl { get; set; } = string.Empty;

    [Url]
    public string PosterUrl { get; set; } = string.Empty;

    [StringLength(1000)]
    public string ShortDescription { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "At least one platform is required")]
    public ICollection<string> Platforms { get; set; } = [];
}

public class UpdateGameRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    [MinLength(1)]
    public ICollection<string> Genres { get; set; } = [];

    [Range(0, ulong.MaxValue)]
    public ulong Followers { get; set; }

    [Required, Url]
    public string StoreUrl { get; set; } = string.Empty;

    [Url]
    public string PosterUrl { get; set; } = string.Empty;

    [StringLength(1000)]
    public string ShortDescription { get; set; } = string.Empty;

    [MinLength(1)]
    public ICollection<string> Platforms { get; set; } = [];
}

/// <summary>
/// Response DTOs
/// </summary>

public class GameResponse
{
    public Guid Id { get; init; }
    public ulong AppId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public ICollection<string> Genres { get; set; } = [];
    public ulong Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public ICollection<string> Platforms { get; set; } = [];
    public DateTime CollectedAt { get; set; }

    public static GameResponse FromEntity(Game game) =>
        new GameResponse
        {
            Id = game.Id,
            AppId = game.AppId,
            Name = game.Name,
            ReleaseDate = game.ReleaseDate,
            Genres = game.Genres?.ToList() ?? [],
            Followers = game.Followers,
            StoreUrl = game.StoreUrl,
            PosterUrl = game.PosterUrl,
            ShortDescription = game.ShortDescription,
            Platforms = game.Platforms?.ToList() ?? [],
            CollectedAt = game.CollectedAt
        };
}

/// <summary>
/// Analytics DTOs
/// </summary>

public class CalendarDayResponse
{
    public DateTime Date { get; init; }
    public int Count { get; set; }
}

public class CalendarResponse
{
    public string Month { get; set; } = string.Empty;
    public ICollection<CalendarDayResponse> Days { get; set; } = [];
}

public class GenreStatsResponse
{
    public string Genre { get; set; } = string.Empty;
    public int Games { get; init; }
    public double AvgFollowers { get; set; }
}

public class GenreDynamicsResponse
{
    public string Month { get; set; } = string.Empty;
    public ICollection<GenreStatsResponse> TopGenres { get; set; } = [];
}

/// <summary>
/// Вспомогательные DTO
/// </summary>

public class SteamSearchResult
{
    public ulong AppId { get; init; }
    public DateTime? ReleaseDate { get; init; }
}

public class GenreDynamicsSeriesResponse
{
    public string Genre { get; set; } = string.Empty;
    public List<int> Counts { get; set; } = [];
    public List<int> AvgFollowers { get; set; } = [];
}

public class GenreDynamicsResultResponse
{
    public List<string> Months { get; set; } = [];
    public List<GenreDynamicsSeriesResponse> Series { get; set; } = [];
}