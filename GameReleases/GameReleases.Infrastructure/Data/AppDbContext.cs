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

            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasKey(g => g.Id);

                entity.HasIndex(g => g.AppId).IsUnique();

                entity.Property(g => g.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(g => g.AppId)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(g => g.StoreUrl).HasMaxLength(500);
                entity.Property(g => g.PosterUrl).HasMaxLength(500);
                entity.Property(g => g.ShortDescription).HasMaxLength(1000);

                // Универсальный компаратор для HashSet<string>
                var collectionComparer = new ValueComparer<ICollection<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => (ICollection<string>)c.ToList()
                );

                entity.Property(g => g.Genres)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => (ICollection<string>)v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        collectionComparer);

                entity.Property(g => g.Platforms)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => (ICollection<string>)v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        collectionComparer);


                entity.HasIndex(g => g.ReleaseDate);
                entity.HasIndex(g => g.CollectedAt);
                entity.HasIndex(g => g.Followers);

                entity.Property(g => g.ReleaseDate).IsRequired(false);
                entity.Property(g => g.CollectedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
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
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList()
                    )
                    .HasColumnType("text");
            });
        }
    }
}
