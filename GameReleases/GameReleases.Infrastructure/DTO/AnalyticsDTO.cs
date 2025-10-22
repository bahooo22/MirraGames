namespace GameReleases.Infrastructure.DTO;

public record DailyReleaseCount(DateTime Date, int Count);
public record GenreStats(string Genre, int GamesCount, double AvgFollowers);
public record GenreDynamics(string Genre, string Month, int GamesCount, double AvgFollowers);