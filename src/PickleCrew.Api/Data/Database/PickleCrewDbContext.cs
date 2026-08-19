using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Data.Database;

public sealed class PickleCrewDbContext(DbContextOptions<PickleCrewDbContext> options)
    : DbContext(options)
{
    public DbSet<Crew> Crews => Set<Crew>();

    public DbSet<CrewProvisioningAttempt> CrewProvisioningAttempts => Set<CrewProvisioningAttempt>();

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
