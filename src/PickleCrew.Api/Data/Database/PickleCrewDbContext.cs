using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Data.Database;

public sealed class PickleCrewDbContext(DbContextOptions<PickleCrewDbContext> options)
    : DbContext(options)
{
    public DbSet<Crew> Crews => Set<Crew>();

    public DbSet<CrewProvisioningAttempt> CrewProvisioningAttempts => Set<CrewProvisioningAttempt>();

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
    }
}
