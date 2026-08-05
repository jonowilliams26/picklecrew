using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Data.Database;

public sealed class PickleCrewDbContext(DbContextOptions<PickleCrewDbContext> options)
    : DbContext(options)
{
    public DbSet<PersistenceProbe> PersistenceProbes => Set<PersistenceProbe>();
}
