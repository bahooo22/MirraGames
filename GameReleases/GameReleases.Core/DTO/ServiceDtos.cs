namespace GameReleases.Core.DTO;

// Request DTOs
public class CreateGameRequest
{
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public HashSet<string> Genres { get; set; } = new HashSet<string>();
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public HashSet<string> Platforms { get; set; } = new HashSet<string>();
}

public class UpdateGameRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public HashSet<string> Genres { get; set; } = new HashSet<string>();
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public HashSet<string> Platforms { get; set; } = new HashSet<string>();
}

// Response DTO
public class GameResponse
{
    public Guid Id { get; set; }
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime? ReleaseDate { get; set; }
    public HashSet<string> Genres { get; set; } = new HashSet<string>();
    public int Followers { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string PosterUrl { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public HashSet<string> Platforms { get; set; } = new HashSet<string>();
    public DateTime CollectedAt { get; set; }
}

// DTO для аналитических эндпоинтов
public class CalendarDayResponse
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class CalendarResponse
{
    public string Month { get; set; } = string.Empty;
    public HashSet<CalendarDayResponse> Days { get; set; } = new HashSet<CalendarDayResponse>();
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
    public HashSet<GenreStatsResponse> TopGenres { get; set; } = new HashSet<GenreStatsResponse>();
}

public class SteamSearchResult
{
    public string AppId { get; set; }
    public DateTime? ReleaseDate { get; set; }
}