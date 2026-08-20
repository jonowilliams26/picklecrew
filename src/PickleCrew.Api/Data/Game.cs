namespace PickleCrew.Api.Data;

public sealed class Game
{
    public long Id { get; private set; }

    public Guid Uuid { get; init; } = Guid.NewGuid();

    public long CrewId { get; init; }

    public Guid CrewUuid { get; init; }

    public Crew Crew { get; init; } = null!;

    public GameFormat Format { get; init; }

    public required string RecorderUserId { get; init; }

    public DateTimeOffset PlayedAtUtc { get; init; }

    public List<GameSide> Sides { get; init; } = [];
}

public sealed class GameSide
{
    public int SideNumber { get; init; }

    public int Score { get; init; }

    public List<GameParticipant> Participants { get; init; } = [];
}

public sealed class GameParticipant
{
    public int SlotNumber { get; init; }

    public string? UserId { get; init; }

    public string? GuestName { get; init; }
}

public enum GameFormat
{
    Singles,
    Doubles,
}
