namespace PickleCrew.Api.Data;

public sealed class CrewMemberWinStreak
{
    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public required long CurrentWinStreak { get; set; }

    public required DateTimeOffset? CurrentWinStreakStartedAtUtc { get; set; }

    public required long LongestWinStreak { get; set; }

    public required DateTimeOffset? LongestWinStreakStartedAtUtc { get; set; }

    public required DateTimeOffset? LongestWinStreakEndedAtUtc { get; set; }

    public uint Version { get; private set; }
}
