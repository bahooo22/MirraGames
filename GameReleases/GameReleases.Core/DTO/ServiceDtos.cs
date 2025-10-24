namespace GameReleases.Core.DTO;

// =======================
// Request DTOs
// =======================

public class CreateGameRequest
{
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public ICollection<string> Genres { get; set; } = [];
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public ICollection<string> Platforms { get; set; } = [];
}

public class UpdateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public ICollection<string> Genres { get; set; } = [];
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public ICollection<string> Platforms { get; set; } = [];
}

// =======================
// Response DTOs
// =======================

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

// =======================
// Analytics DTOs
// =======================

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

// =======================
// Вспомогательные DTO
// =======================

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
