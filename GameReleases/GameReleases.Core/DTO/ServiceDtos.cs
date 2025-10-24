using System.ComponentModel.DataAnnotations;

namespace GameReleases.Core.DTO;

/// <summary>
/// Request DTOs
/// </summary>

public class CreateGameRequest
{
    [Required, StringLength(50)]
    public string AppId { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    [MinLength(1, ErrorMessage = "At least one genre is required")]
    public ICollection<string> Genres { get; set; } = [];

    [Range(0, int.MaxValue, ErrorMessage = "Followers must be non-negative")]
    public int Followers { get; set; }

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

    [Range(0, int.MaxValue)]
    public int Followers { get; set; }

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
    public Guid Id { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public ICollection<string> Genres { get; set; } = [];
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public ICollection<string> Platforms { get; set; } = [];
    public DateTime CollectedAt { get; set; }

    public static GameResponse FromEntity(GameReleases.Infrastructure.Entities.Game game) =>
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
    public DateTime Date { get; set; }
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
    public int Games { get; set; }
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
    public string AppId { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
}

public class GenreDynamicsSeriesResponse
{
    public string Genre { get; set; } = string.Empty;
    public List<int> Counts { get; set; } = new();
    public List<int> AvgFollowers { get; set; } = new();
}

public class GenreDynamicsResultResponse
{
    public List<string> Months { get; set; } = new();
    public List<GenreDynamicsSeriesResponse> Series { get; set; } = new();
}
