using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class GameStatisticsService(CrewScopedDbContext database)
{
    public async Task<CalculateGameStatisticsResult> CalculateStatisticsAsync(
        Game game,
        CancellationToken cancellationToken
    )
    {
        var playerStatistics = await CalculatePlayerStatistics(game, cancellationToken);
        var doublesPairingStatistics = await CalculateDoublesPairingStatistics(game, cancellationToken);
        var matchupStatistics = await CalculateMatchupStatistics(game, cancellationToken);
        return new CalculateGameStatisticsResult
        {
            PlayerStatistics = playerStatistics,
            DoublesPairingStatistics = doublesPairingStatistics,
            MatchupStatistics = matchupStatistics
        };
    }

    private async Task<CalculateGameStatisticsResult<CrewMemberStatistics>[]> CalculatePlayerStatistics(Game game, CancellationToken cancellationToken)
    {
        var gameStatistics = game.Sides
            .SelectMany(
                side => side.Participants,
                (side, participant) => new
                {
                    Participant = participant,
                    Side = side,
                }
            )
            .Where(x => x.Participant.UserId is not null)
            .Select(x =>
            {
                var opposingSide = game.Sides.First(side => side != x.Side);
                return new PlayerGameStatistics
                {
                    UserId = x.Participant.UserId!,
                    Won = x.Side.Score > opposingSide.Score,
                    IsThatsAPaddlin = x.Side.Score == 11 && opposingSide.Score == 0,
                    PointsScored = x.Side.Score,
                    PointsConceded = opposingSide.Score,
                };
            })
            .ToArray();

        var playerIds = gameStatistics.Select(x => x.UserId)
            .ToArray();

        var statisticsByUserId = await database.CrewMemberStatistics
            .Where(statistics => playerIds.Contains(statistics.UserId))
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var results = new List<CalculateGameStatisticsResult<CrewMemberStatistics>>();

        foreach (var playerGameStatistics in gameStatistics)
        {
            var operation = CalculateGameStatisticsResultOperation.Updated;
            if (!statisticsByUserId.TryGetValue(playerGameStatistics.UserId, out var statistics))
            {
                statistics = new CrewMemberStatistics
                {
                    UserId = playerGameStatistics.UserId,
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                };
                database.CrewMemberStatistics.Add(statistics);
                operation = CalculateGameStatisticsResultOperation.Created;
            }

            statistics.Wins += playerGameStatistics.Won ? 1 : 0;
            statistics.Losses += playerGameStatistics.Won ? 0 : 1;
            statistics.PointsScored += playerGameStatistics.PointsScored;
            statistics.PointsConceded += playerGameStatistics.PointsConceded;
            statistics.ThatsAPaddlin += playerGameStatistics.IsThatsAPaddlin ? 1 : 0;
            statistics.CrewScore += CrewScoreCalculator.Calculate(
                playerGameStatistics.Won,
                playerGameStatistics.PointsScored,
                playerGameStatistics.PointsConceded
            );

            if (playerGameStatistics.Won)
            {
                statistics.CurrentWinStreak += 1;
                statistics.CurrentWinStreakStartedAtUtc ??= game.PlayedAtUtc;

                if (statistics.CurrentWinStreak > statistics.LongestWinStreak)
                {
                    statistics.LongestWinStreak = statistics.CurrentWinStreak;
                    statistics.LongestWinStreakStartedAtUtc = statistics.CurrentWinStreakStartedAtUtc;
                    statistics.LongestWinStreakEndedAtUtc = null;
                }
            }
            else
            {
                var endedRecordSettingStreak =
                    statistics.CurrentWinStreak > 0 &&
                    statistics.CurrentWinStreak == statistics.LongestWinStreak &&
                    statistics.CurrentWinStreakStartedAtUtc == statistics.LongestWinStreakStartedAtUtc &&
                    statistics.LongestWinStreakEndedAtUtc is null;

                if (endedRecordSettingStreak)
                {
                    statistics.LongestWinStreakEndedAtUtc = game.PlayedAtUtc;
                }

                statistics.CurrentWinStreak = 0;
                statistics.CurrentWinStreakStartedAtUtc = null;
            }

            var result = new CalculateGameStatisticsResult<CrewMemberStatistics>
            {
                Operation = operation,
                Statistics = statistics,
            };

            results.Add(result);
        }

        return [.. results];
    }

    private async Task<CalculateGameStatisticsResult<CrewDoublesPairingStatistics>[]> CalculateDoublesPairingStatistics(Game game, CancellationToken cancellationToken)
    {
        if (game.Format is not GameFormat.Doubles)
        {
            return [];
        }

        var doublesPairingGameStatistics = game.Sides
            .Where(side => side.Participants.All(participant => participant.UserId is not null))
            .Select(side =>
            {
                var opposingSide = game.Sides.First(candidate => candidate != side);
                var key = CrewDoublesPairingKey.Create(
                    side.Participants[0].UserId!,
                    side.Participants[1].UserId!
                );

                return new DoublesPairingGameStatistics
                {
                    Key = key,
                    Won = side.Score > opposingSide.Score,
                    PointsScored = side.Score,
                    PointsConceded = opposingSide.Score,
                };
            })
            .ToArray();

        if (doublesPairingGameStatistics.Length == 0)
        {
            return [];
        }

        var playerIds = doublesPairingGameStatistics
            .SelectMany(x => new[] { x.Key.Player1Id, x.Key.Player2Id })
            .ToArray();

        var doublesPairingStatisticsByKey = await database.CrewDoublesPairingStatistics
            .Where(statistics =>
                playerIds.Contains(statistics.Player1Id) &&
                playerIds.Contains(statistics.Player2Id)
            )
            .ToDictionaryAsync(
                statistics => CrewDoublesPairingKey.Create(
                    statistics.Player1Id,
                    statistics.Player2Id
                ),
                cancellationToken
            );

        var results = new List<CalculateGameStatisticsResult<CrewDoublesPairingStatistics>>();

        foreach (var doublesPairingGameStats in doublesPairingGameStatistics)
        {
            var operation = CalculateGameStatisticsResultOperation.Updated;
            if (!doublesPairingStatisticsByKey.TryGetValue(doublesPairingGameStats.Key, out var statistics))
            {
                statistics = new CrewDoublesPairingStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    Player1Id = doublesPairingGameStats.Key.Player1Id,
                    Player2Id = doublesPairingGameStats.Key.Player2Id,
                };
                database.CrewDoublesPairingStatistics.Add(statistics);
                operation = CalculateGameStatisticsResultOperation.Created;
            }

            statistics.Wins += doublesPairingGameStats.Won ? 1 : 0;
            statistics.Losses += doublesPairingGameStats.Won ? 0 : 1;
            statistics.PointsScored += doublesPairingGameStats.PointsScored;
            statistics.PointsConceded += doublesPairingGameStats.PointsConceded;

            var result = new CalculateGameStatisticsResult<CrewDoublesPairingStatistics>
            {
                Operation = operation,
                Statistics = statistics,
            };

            results.Add(result);
        }

        return [.. results];
    }

    private async Task<CalculateGameStatisticsResult<CrewMatchupStatistics>[]> CalculateMatchupStatistics(Game game, CancellationToken cancellationToken)
    {
        var matchupGameStatistics = game.Sides
            .SelectMany(side =>
            {
                var opposingSide = game.Sides.First(candidate => candidate != side);
                var opposingPlayers = opposingSide.Participants
                    .Where(participant => participant.UserId is not null)
                    .Select(participant => participant.UserId!)
                    .ToArray();

                return side.Participants
                    .Where(participant => participant.UserId is not null)
                    .SelectMany(participant => opposingPlayers.Select(opponentId => new MatchupGameStatistics
                    {
                        UserId = participant.UserId!,
                        OpponentUserId = opponentId,
                        Won = side.Score > opposingSide.Score,
                        PointsScored = side.Score,
                        PointsConceded = opposingSide.Score,
                    }));
            })
            .ToArray();

        if (matchupGameStatistics.Length == 0)
        {
            return [];
        }

        var playerIds = matchupGameStatistics
            .SelectMany(x => new[] { x.UserId, x.OpponentUserId })
            .ToArray();

        var matchupStatisticsByKey = await database.CrewMatchupStatistics
            .Where(statistics =>
                playerIds.Contains(statistics.UserId) &&
                playerIds.Contains(statistics.OpponentUserId)
            )
            .ToDictionaryAsync(
                statistics => new MatchupKey(statistics.UserId, statistics.OpponentUserId),
                cancellationToken
            );

        var results = new List<CalculateGameStatisticsResult<CrewMatchupStatistics>>();
        foreach (var matchupGameStats in matchupGameStatistics)
        {
            var operation = CalculateGameStatisticsResultOperation.Updated;
            var key = new MatchupKey(matchupGameStats.UserId, matchupGameStats.OpponentUserId);
            if (!matchupStatisticsByKey.TryGetValue(key, out var statistics))
            {
                statistics = new CrewMatchupStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = matchupGameStats.UserId,
                    OpponentUserId = matchupGameStats.OpponentUserId,
                };
                database.CrewMatchupStatistics.Add(statistics);
                operation = CalculateGameStatisticsResultOperation.Created;
            }

            statistics.Wins += matchupGameStats.Won ? 1 : 0;
            statistics.Losses += matchupGameStats.Won ? 0 : 1;
            statistics.PointsScored += matchupGameStats.PointsScored;
            statistics.PointsConceded += matchupGameStats.PointsConceded;

            var result = new CalculateGameStatisticsResult<CrewMatchupStatistics>
            {
                Operation = operation,
                Statistics = statistics,
            };

            results.Add(result);
        }

        return [.. results];
    }

    private sealed record PlayerGameStatistics
    {
        public required string UserId { get; init; }
        public bool Won { get; init; }
        public bool IsThatsAPaddlin { get; init; }
        public long PointsScored { get; init; }
        public long PointsConceded { get; init; }
    }

    private sealed record DoublesPairingGameStatistics
    {
        public required CrewDoublesPairingKey Key { get; init; }
        public bool Won { get; init; }
        public long PointsScored { get; init; }
        public long PointsConceded { get; init; }
    }

    private sealed record MatchupGameStatistics
    {
        public required string UserId { get; init; }
        public required string OpponentUserId { get; init; }
        public bool Won { get; init; }
        public long PointsScored { get; init; }
        public long PointsConceded { get; init; }
    }

    private sealed record MatchupKey(string UserId, string OpponentUserId);
}

internal sealed record CalculateGameStatisticsResult
{
    public required CalculateGameStatisticsResult<CrewMemberStatistics>[] PlayerStatistics { get; init; }
    public required CalculateGameStatisticsResult<CrewDoublesPairingStatistics>[] DoublesPairingStatistics { get; init; }
    public required CalculateGameStatisticsResult<CrewMatchupStatistics>[] MatchupStatistics { get; init; }
}

internal sealed record CalculateGameStatisticsResult<T>
{
    public required CalculateGameStatisticsResultOperation Operation { get; init; }
    public required T Statistics { get; init; }
}

internal enum CalculateGameStatisticsResultOperation
{
    Created,
    Updated
}
