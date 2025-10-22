using GameReleases.Infrastructure.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GameReleases.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Game> Games => Set<Game>();
        public DbSet<GameHistory> GameHistories => Set<GameHistory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Config of lists JSON serialization
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(g => g.Id);

                // Уникальный индекс для AppId
                entity.HasIndex(g => g.AppId)
                      .IsUnique();

                // Ограничения на строковые поля
                entity.Property(g => g.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(g => g.AppId)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.StoreUrl)
                      .HasMaxLength(500);

                entity.Property(g => g.PosterUrl)
                      .HasMaxLength(500);

                entity.Property(g => g.ShortDescription)
                      .HasMaxLength(1000);

                // Для хранения коллекций строк как JSON в PostgreSQL с ValueComparer
                var hashSetComparer = new ValueComparer<HashSet<string>>(
                    (c1, c2) => c1 != null && c2 != null ? c1.SetEquals(c2) : c1 == c2,
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new HashSet<string>(c));

                entity.Property(g => g.Genres)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => new HashSet<string>(v.Split(';', StringSplitOptions.RemoveEmptyEntries)),
                        hashSetComparer) // ⭐ ДОБАВЛЕН ValueComparer
                    .HasColumnType("text")
                    .HasMaxLength(1000);

                entity.Property(g => g.Platforms)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => new HashSet<string>(v.Split(';', StringSplitOptions.RemoveEmptyEntries)),
                        hashSetComparer) // ⭐ ДОБАВЛЕН ValueComparer
                    .HasColumnType("text")
                    .HasMaxLength(500);

                // Индексы для производительности
                entity.HasIndex(g => g.ReleaseDate);
                entity.HasIndex(g => g.CollectedAt);
                entity.HasIndex(g => g.Followers);

                entity.Property(g => g.ReleaseDate)
                      .IsRequired(false);

                entity.Property(g => g.CollectedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<GameHistory>(entity =>
            {
                entity.HasKey(gh => gh.Id);
                entity.HasOne(gh => gh.Game)
                    .WithMany()
                    .HasForeignKey(gh => gh.GameId);

                entity.Property(gh => gh.Genres)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet());
            });
        }
    }
}
