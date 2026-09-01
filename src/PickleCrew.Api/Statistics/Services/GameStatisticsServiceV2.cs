using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class GameStatisticsServiceV2(CrewScopedDbContext database)
{
    public async Task CalculateStatisticsAsync(
        Game game,
        CancellationToken cancellationToken = default
    )
    {
        CalculatePlayerStatistics(game);
        CalculateDoublesPairingStatistics(game);
        CalculateMatchupStatistics(game);
        await CalculateWinStreaksAsync(game, cancellationToken);
    }

    private void CalculatePlayerStatistics(Game game)
    {
        var statistics = game.Sides
            .SelectMany(
                side => side.Participants,
                (side, participant) => new
                {
                    UserId = participant.UserId ?? "",
                    Score = side.Score,
                    SideNumber = side.SideNumber
                }
            )
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .Select(x =>
            {
                var opposingSide = game.Sides.First(side => side.SideNumber != x.SideNumber);
                var won = x.Score > opposingSide.Score;
                var pointDifferential = x.Score - opposingSide.Score;

                return new CrewMemberGameStatistics
                {
                    Game = game,
                    GameId = game.Id,
                    GameUuid = game.Uuid,
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = x.UserId,
                    PlayedAtUtc = game.PlayedAtUtc,
                    Wins = won ? 1 : 0,
                    Losses = !won ? 1 : 0,
                    PointsScored = x.Score,
                    PointsConceded = opposingSide.Score,
                    PointDifferential = pointDifferential,
                    CrewScore = won ? (100 + pointDifferential) : 0,
                    ThatsAPaddlin = x.Score == 11 && opposingSide.Score == 0 ? 1 : 0
                };
            })
            .ToArray();

        database.CrewMemberGameStatistics.AddRange(statistics);
    }

    private void CalculateDoublesPairingStatistics(Game game)
    {
        if (game.Format is not GameFormat.Doubles)
        {
            return;
        }

        var statistics = game.Sides
            .Where(side => side.Participants.All(participant => participant.UserId is not null))
            .Select(side =>
            {
                var opposingSide = game.Sides.First(candidate => candidate != side);
                var key = CrewDoublesPairingKey.Create(
                    side.Participants[0].UserId!,
                    side.Participants[1].UserId!
                );

                return new CrewDoublesPairingGameStatistics
                {
                    Game = game,
                    GameId = game.Id,
                    GameUuid = game.Uuid,
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    Player1Id = key.Player1Id,
                    Player2Id = key.Player2Id,
                    PlayedAtUtc = game.PlayedAtUtc,
                    Wins = side.Score > opposingSide.Score ? 1 : 0,
                    Losses = side.Score < opposingSide.Score ? 1 : 0,
                    PointsScored = side.Score,
                    PointsConceded = opposingSide.Score,
                    PointDifferential = side.Score - opposingSide.Score,
                };
            })
            .ToArray();

        database.CrewDoublesPairingGameStatistics.AddRange(statistics);
    }

    private void CalculateMatchupStatistics(Game game)
    {
        var statistics = game.Sides
            .SelectMany(side =>
            {
                var opposingSide = game.Sides.First(candidate => candidate != side);
                var opposingPlayers = opposingSide.Participants
                    .Where(participant => participant.UserId is not null)
                    .Select(participant => participant.UserId!)
                    .ToArray();

                return side.Participants
                    .Where(participant => participant.UserId is not null)
                    .SelectMany(participant => opposingPlayers.Select(opponentId => new CrewMatchupGameStatistics
                    {
                        Game = game,
                        GameId = game.Id,
                        GameUuid = game.Uuid,
                        CrewId = game.CrewId,
                        CrewUuid = game.CrewUuid,
                        UserId = participant.UserId!,
                        OpponentUserId = opponentId,
                        PlayedAtUtc = game.PlayedAtUtc,
                        Wins = side.Score > opposingSide.Score ? 1 : 0,
                        Losses = side.Score < opposingSide.Score ? 1 : 0,
                        PointsScored = side.Score,
                        PointsConceded = opposingSide.Score,
                        PointDifferential = side.Score - opposingSide.Score,
                    }));
            })
            .ToArray();

        database.CrewMatchupGameStatistics.AddRange(statistics);
    }

    private async Task CalculateWinStreaksAsync(Game game, CancellationToken cancellationToken)
    {
        var resultsByUserId = game.Sides
            .SelectMany(
                side => side.Participants,
                (side, participant) =>
                {
                    var opposingSide = game.Sides.First(otherSide => otherSide != side);
                    var won = side.Score > opposingSide.Score;
                    return new
                    {
                        UserId = participant.UserId ?? "",
                        Won = won
                    };
                }
            )
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .ToDictionary(x => x.UserId, x => x.Won);

        var winStreaksByUserId = await database.CrewMemberWinStreaks
            .Where(x => resultsByUserId.Keys.Contains(x.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        foreach (var (userId, won) in resultsByUserId)
        {
            if (!winStreaksByUserId.TryGetValue(userId, out var winStreak))
            {
                winStreak = new CrewMemberWinStreak
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = userId,
                    CurrentWinStreak = won ? 1 : 0,
                    CurrentWinStreakStartedAtUtc = won ? game.PlayedAtUtc : null,
                    LongestWinStreak = won ? 1 : 0,
                    LongestWinStreakStartedAtUtc = won ? game.PlayedAtUtc : null,
                };

                database.CrewMemberWinStreaks.Add(winStreak);
                continue;
            }

            if (won)
            {
                winStreak.CurrentWinStreak++;
                winStreak.CurrentWinStreakStartedAtUtc ??= game.PlayedAtUtc;

                if (winStreak.CurrentWinStreak > winStreak.LongestWinStreak)
                {
                    winStreak.LongestWinStreak = winStreak.CurrentWinStreak;
                    winStreak.LongestWinStreakStartedAtUtc = winStreak.CurrentWinStreakStartedAtUtc;
                    winStreak.LongestWinStreakEndedAtUtc = null;
                }
            }
            else
            {
                var endedRecordSettingStreak =
                    winStreak.CurrentWinStreak > 0 &&
                    winStreak.CurrentWinStreak == winStreak.LongestWinStreak &&
                    winStreak.CurrentWinStreakStartedAtUtc == winStreak.LongestWinStreakStartedAtUtc &&
                    winStreak.LongestWinStreakEndedAtUtc is null;

                if (endedRecordSettingStreak)
                {
                    winStreak.LongestWinStreakEndedAtUtc = game.PlayedAtUtc;
                }

                winStreak.CurrentWinStreak = 0;
                winStreak.CurrentWinStreakStartedAtUtc = null;
            }
        }
    }
}
