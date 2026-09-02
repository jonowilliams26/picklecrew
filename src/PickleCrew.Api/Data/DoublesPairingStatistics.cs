namespace PickleCrew.Api.Data;

public sealed class DoublesPairingStatistics
{
    public required long GameId { get; init; }

    public required Guid GameUuid { get; init; }

    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string Player1Id { get; init; }

    public required string Player2Id { get; init; }

    public Game Game { get; init; } = null!;

    public Crew Crew { get; init; } = null!;

    public required DateTimeOffset PlayedAtUtc { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }
}

internal sealed record CrewDoublesPairingKey
{
    private CrewDoublesPairingKey(string player1Id, string player2Id)
    {
        Player1Id = player1Id;
        Player2Id = player2Id;
    }

    public string Player1Id { get; private init; }

    public string Player2Id { get; private init; }

    public static CrewDoublesPairingKey Create(
        string firstPlayerId,
        string secondPlayerId)
    {
        ArgumentNullException.ThrowIfNull(firstPlayerId);
        ArgumentNullException.ThrowIfNull(secondPlayerId);

        if (StringComparer.Ordinal.Equals(firstPlayerId, secondPlayerId))
        {
            throw new ArgumentException(
                "A doubles pairing must contain two different players.",
                nameof(secondPlayerId)
            );
        }

        return StringComparer.Ordinal.Compare(firstPlayerId, secondPlayerId) < 0
            ? new CrewDoublesPairingKey(firstPlayerId, secondPlayerId)
            : new CrewDoublesPairingKey(secondPlayerId, firstPlayerId);
    }
}
