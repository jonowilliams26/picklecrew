using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Data.Database;

public class PickleCrewDbContext : DbContext
{
    public PickleCrewDbContext(DbContextOptions<PickleCrewDbContext> options)
        : base(options)
    {
    }

    protected PickleCrewDbContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Crew> Crews => Set<Crew>();

    public DbSet<CrewProvisioningAttempt> CrewProvisioningAttempts => Set<CrewProvisioningAttempt>();

    public DbSet<PlayerStatistics> PlayerStatistics => Set<PlayerStatistics>();

    public DbSet<MatchupStatistics> MatchupStatistics => Set<MatchupStatistics>();

    public DbSet<DoublesPairingStatistics> DoublesPairingStatistics => Set<DoublesPairingStatistics>();

    public DbSet<PlayerWinStreak> PlayerWinStreaks => Set<PlayerWinStreak>();

    public DbSet<Game> Games => Set<Game>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Crew>(crew =>
        {
            crew.HasIndex(entity => entity.Uuid).IsUnique();
            crew.HasIndex(entity => entity.WorkOsOrganizationId).IsUnique();
        });

        modelBuilder.Entity<CrewProvisioningAttempt>(attempt =>
        {
            attempt.HasKey(entity => entity.CrewUuid);
            attempt.HasIndex(entity => new
            {
                entity.WorkOsUserId,
                entity.RequestId,
            }).IsUnique();
            attempt.HasOne<Crew>()
                .WithOne()
                .HasPrincipalKey<CrewProvisioningAttempt>(candidate => candidate.CrewUuid)
                .HasForeignKey<Crew>(crew => crew.Uuid)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlayerStatistics>(statistics =>
        {
            statistics.HasKey(entity => new
            {
                entity.GameId,
                entity.UserId,
            });
            statistics.HasIndex(entity => new
            {
                entity.CrewId,
                entity.UserId,
            });
            statistics.HasOne(entity => entity.Game)
                .WithMany()
                .HasForeignKey(entity => entity.GameId);
            statistics.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
        });

        modelBuilder.Entity<MatchupStatistics>(statistics =>
        {
            statistics.HasKey(entity => new
            {
                entity.GameId,
                entity.UserId,
                entity.OpponentUserId,
            });
            statistics.HasIndex(entity => new
            {
                entity.CrewId,
                entity.UserId,
                entity.OpponentUserId,
            });
            statistics.HasOne(entity => entity.Game)
                .WithMany()
                .HasForeignKey(entity => entity.GameId);
            statistics.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
        });

        modelBuilder.Entity<DoublesPairingStatistics>(statistics =>
        {
            statistics.HasKey(entity => new
            {
                entity.GameId,
                entity.Player1Id,
                entity.Player2Id,
            });
            statistics.HasIndex(entity => new
            {
                entity.CrewId,
                entity.Player1Id,
                entity.Player2Id,
            });
            statistics.HasIndex(entity => new
            {
                entity.CrewId,
                entity.Player2Id,
                entity.Player1Id,
            });
            statistics.HasOne(entity => entity.Game)
                .WithMany()
                .HasForeignKey(entity => entity.GameId);
            statistics.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
        });

        modelBuilder.Entity<PlayerWinStreak>(winStreak =>
        {
            winStreak.HasKey(entity => new
            {
                entity.CrewId,
                entity.UserId,
            });
            winStreak.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
            winStreak.Property(entity => entity.Version)
                .IsRowVersion();
        });

        modelBuilder.Entity<Game>(game =>
        {
            game.HasIndex(entity => entity.Uuid).IsUnique();
            game.HasIndex(entity => new
            {
                entity.CrewId,
                entity.PlayedAtUtc,
            });
            game.Property(entity => entity.Format).HasConversion<string>();
            game.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
            game.ComplexCollection(entity => entity.Sides, sides =>
            {
                sides.ToJson();
                sides.ComplexCollection(side => side.Participants);
            });
        });
    }
}

internal sealed class CrewScopedDbContext(
    DbContextOptions<CrewScopedDbContext> options,
    CrewUserContext crewUserContext)
    : PickleCrewDbContext(options)
{
    private long CrewId => crewUserContext.CrewId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerStatistics>()
            .HasQueryFilter(statistics => statistics.CrewId == CrewId);
        modelBuilder.Entity<MatchupStatistics>()
            .HasQueryFilter(statistics => statistics.CrewId == CrewId);
        modelBuilder.Entity<DoublesPairingStatistics>()
            .HasQueryFilter(statistics => statistics.CrewId == CrewId);
        modelBuilder.Entity<PlayerWinStreak>()
            .HasQueryFilter(winStreak => winStreak.CrewId == CrewId);
        modelBuilder.Entity<Game>()
            .HasQueryFilter(game => game.CrewId == CrewId);
    }
}
