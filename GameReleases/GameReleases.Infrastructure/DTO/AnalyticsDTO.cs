namespace GameReleases.Infrastructure.DTO;

// DTO для аналитики и ClickHouse

/// <summary>
/// Количество релизов по дням.
/// </summary>
public class DailyReleaseCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }

    public DailyReleaseCount() { }
}

/// <summary>
/// Статистика по жанрам (агрегация).
/// </summary>
public record GenreStats(string Genre, int GamesCount, double AvgFollowers);

/// <summary>
/// Динамика по жанрам по месяцам.
/// </summary>
public class GenreDynamics
{
    public string Genre { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public int GamesCount { get; set; }
    public double AvgFollowers { get; set; }

    public GenreDynamics() { }
}

/// <summary>
/// DTO для маппинга из ClickHouse (обязательно нужен пустой конструктор).
/// </summary>
public class GenreAnalytics
{
    public string Genre { get; set; } = string.Empty;
    public int GameCount { get; set; }
    public double AvgFollowers { get; set; }
    public DateTime Period { get; set; }

    public GenreAnalytics() { }
}