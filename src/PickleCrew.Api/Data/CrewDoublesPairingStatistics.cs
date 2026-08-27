namespace PickleCrew.Api.Data;

public sealed class CrewDoublesPairingStatistics
{
    public long CrewId { get; init; }

    public Guid CrewUuid { get; init; }

    public required string Player1Id { get; init; }

    public required string Player2Id { get; init; }

    public Crew Crew { get; init; } = null!;

    public long Wins { get; set; }

    public long Losses { get; set; }

    public long PointsScored { get; set; }

    public long PointsConceded { get; set; }

    public uint Version { get; private set; }
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
