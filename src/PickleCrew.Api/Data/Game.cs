namespace PickleCrew.Api.Data;

public sealed class Game
{
    public long Id { get; private set; }

    public Guid Uuid { get; init; } = Guid.NewGuid();

    public long CrewId { get; init; }

    public Crew Crew { get; init; } = null!;

    public GameFormat Format { get; set; }

    public required string RecorderUserId { get; init; }

    public DateTimeOffset PlayedAtUtc { get; init; }

    public List<GameSide> Sides { get; } = [];
}

public sealed class GameSide
{
    public int SideNumber { get; init; }

    public int Score { get; set; }

    public List<GameParticipant> Participants { get; } = [];
}

public sealed class GameParticipant
{
    public int SlotNumber { get; set; }

    public string? UserId { get; set; }

    public string? GuestName { get; set; }
}

public enum GameFormat
{
    Singles,
    Doubles,
}
