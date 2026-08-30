namespace PickleCrew.Api.Data;

public sealed class CrewProvisioningAttempt
{
    public Guid CrewUuid { get; init; }

    public required Guid RequestId { get; init; }

    public required string CrewName { get; init; }

    public required string WorkOsUserId { get; init; }

    public string? WorkOsOrganizationId { get; set; }

    public string? WorkOsMembershipId { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
