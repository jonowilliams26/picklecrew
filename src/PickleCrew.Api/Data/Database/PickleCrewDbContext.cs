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

    public DbSet<CrewMemberStatistics> CrewMemberStatistics => Set<CrewMemberStatistics>();

    public DbSet<CrewDoublesPairingStatistics> CrewDoublesPairingStatistics => Set<CrewDoublesPairingStatistics>();

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
            attempt.HasIndex(entity => entity.WorkOsUserId).IsUnique();
            attempt.HasOne<Crew>()
                .WithOne()
                .HasPrincipalKey<CrewProvisioningAttempt>(candidate => candidate.CrewUuid)
                .HasForeignKey<Crew>(crew => crew.Uuid)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CrewMemberStatistics>(statistics =>
        {
            statistics.HasKey(entity => new
            {
                entity.CrewId,
                entity.UserId,
            });
            statistics.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
            statistics.Property(entity => entity.Version)
                .IsRowVersion();
        });

        modelBuilder.Entity<CrewDoublesPairingStatistics>(statistics =>
        {
            statistics.HasKey(entity => new
            {
                entity.CrewId,
                entity.Player1Id,
                entity.Player2Id,
            });
            statistics.HasOne(entity => entity.Crew)
                .WithMany()
                .HasForeignKey(entity => entity.CrewId);
            statistics.Property(entity => entity.Version)
                .IsRowVersion();
        });

        modelBuilder.Entity<Game>(game =>
        {
            game.HasIndex(entity => entity.Uuid).IsUnique();
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

        modelBuilder.Entity<CrewMemberStatistics>()
            .HasQueryFilter(statistics => statistics.CrewId == CrewId);
        modelBuilder.Entity<CrewDoublesPairingStatistics>()
            .HasQueryFilter(statistics => statistics.CrewId == CrewId);
        modelBuilder.Entity<Game>()
            .HasQueryFilter(game => game.CrewId == CrewId);
    }
}
