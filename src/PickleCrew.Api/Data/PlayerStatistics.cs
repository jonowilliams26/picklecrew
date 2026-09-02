namespace PickleCrew.Api.Data;

public sealed class PlayerStatistics
{
    public required long GameId { get; init; }

    public required Guid GameUuid { get; init; }

    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Game Game { get; init; } = null!;

    public Crew Crew { get; init; } = null!;

    public required DateTimeOffset PlayedAtUtc { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }

    public required int CrewScore { get; init; }

    public required int ThatsAPaddlin { get; init; }
}
