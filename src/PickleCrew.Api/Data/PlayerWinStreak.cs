namespace PickleCrew.Api.Data;

public sealed class PlayerWinStreak
{
    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }

    public required string UserId { get; init; }

    public Crew Crew { get; init; } = null!;

    public int CurrentWinStreak { get; set; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; set; }

    public int LongestWinStreak { get; set; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; set; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; set; }

    public uint Version { get; private set; }

    public void ApplyGameResult(Game game)
    {
        var side = game.Sides.SingleOrDefault(side =>
            side.Participants.Any(participant => participant.UserId == UserId)
        );

        if (side is null)
        {
            return;
        }

        var opposingSide = game.Sides.Single(candidate => candidate != side);
        var won = side.Score > opposingSide.Score;

        if (won)
        {
            CurrentWinStreak++;
            CurrentWinStreakStartedAtUtc ??= game.PlayedAtUtc;

            if (CurrentWinStreak > LongestWinStreak)
            {
                LongestWinStreak = CurrentWinStreak;
                LongestWinStreakStartedAtUtc = CurrentWinStreakStartedAtUtc;
                LongestWinStreakEndedAtUtc = null;
            }

            return;
        }

        var endedRecordSettingStreak =
            CurrentWinStreak > 0
            && CurrentWinStreak == LongestWinStreak
            && CurrentWinStreakStartedAtUtc == LongestWinStreakStartedAtUtc
            && LongestWinStreakEndedAtUtc is null;

        if (endedRecordSettingStreak)
        {
            LongestWinStreakEndedAtUtc = game.PlayedAtUtc;
        }

        CurrentWinStreak = 0;
        CurrentWinStreakStartedAtUtc = null;
    }
}
