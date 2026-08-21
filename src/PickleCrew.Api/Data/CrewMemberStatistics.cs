namespace PickleCrew.Api.Data;

public sealed class CrewMemberStatistics
{
    public long CrewId { get; init; }

    public Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public long Wins { get; set; }

    public long Losses { get; set; }

    public long PointsScored { get; set; }

    public long PointsConceded { get; set; }

    public uint Version { get; private set; }
}
