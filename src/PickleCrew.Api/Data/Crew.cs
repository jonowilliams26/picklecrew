namespace PickleCrew.Api.Data;

public sealed class Crew
{
    public long Id { get; private set; }

    public Guid Uuid { get; init; } = Guid.NewGuid();

    public required string Name { get; set; }

    public required string WorkOsOrganizationId { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }
}
