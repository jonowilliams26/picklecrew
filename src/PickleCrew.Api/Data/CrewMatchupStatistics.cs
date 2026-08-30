namespace PickleCrew.Api.Data;

public sealed class CrewMatchupStatistics
{
    public long CrewId { get; init; }

    public Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public required string OpponentUserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public long Wins { get; set; }

    public long Losses { get; set; }

    public long PointsScored { get; set; }

    public long PointsConceded { get; set; }

    // EF Core requires setters to persist these calculated properties.
    public long GamesPlayed
    {
        get => Wins + Losses;
        private set { }
    }

    public decimal WinPercentage
    {
        get => GamesPlayed == 0
            ? 0
            : (decimal)Wins / GamesPlayed;
        private set { }
    }

    public long PointDifferential
    {
        get => PointsScored - PointsConceded;
        private set { }
    }

    public uint Version { get; private set; }
}
