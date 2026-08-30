namespace PickleCrew.Api.Data;

public sealed class CrewMemberStatistics
{
    public long CrewId { get; init; }

    public Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public long Wins { get; set; }

    public long Losses { get; set; }

    public long ThatsAPaddlin { get; set; }

    // No-op private setters allow EF Core to persist these calculated getters.
    // See https://github.com/dotnet/efcore/issues/13316.
    public long GamesPlayed
    {
        get => Wins + Losses;
        private set
        {
        }
    }

    public decimal WinPercentage
    {
        get => GamesPlayed == 0
            ? 0
            : (decimal)Wins / GamesPlayed;
        private set
        {
        }
    }

    public long CurrentWinStreak { get; set; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; set; }

    public long LongestWinStreak { get; set; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; set; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; set; }

    public long PointsScored { get; set; }

    public long PointsConceded { get; set; }

    public long PointDifferential
    {
        get => PointsScored - PointsConceded;
        private set
        {
        }
    }

    public long CrewScore { get; set; }

    public uint Version { get; private set; }
}
