namespace PickleCrew.Api.Data;

public sealed class CrewMemberWinStreak
{
    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public long CurrentWinStreak { get; set; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; set; }

    public long LongestWinStreak { get; set; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; set; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; set; }

    public uint Version { get; private set; }
}
