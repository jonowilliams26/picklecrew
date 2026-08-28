using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;

namespace PickleCrew.Api.Games.Services;

internal sealed class GameStatisticsService(CrewScopedDbContext database)
{
    public async Task<CalculateGameStatisticsResult> CalculateStatistics(
        Game game,
        CancellationToken cancellationToken)
    {
        var playerGameStatistics = game.Sides
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

        var doublesPairingGameStatistics = game.Format == GameFormat.Doubles
            ? game.Sides
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
                .ToArray()
            : [];

        var matchupGameStatistics = game.Sides
            .SelectMany(side =>
            {
                var opposingSide = game.Sides.First(candidate => candidate != side);

                return side.Participants
                    .Where(participant => participant.UserId is not null)
                    .SelectMany(participant => opposingSide.Participants
                        .Where(opponent => opponent.UserId is not null)
                        .Select(opponent => new MatchupGameStatistics
                        {
                            Key = new CrewMatchupKey(
                                participant.UserId!,
                                opponent.UserId!
                            ),
                            Won = side.Score > opposingSide.Score,
                            PointsScored = side.Score,
                            PointsConceded = opposingSide.Score,
                        })
                    );
            })
            .ToArray();

        var participatingUserIds = playerGameStatistics
            .Select(x => x.UserId)
            .ToArray();

        var statisticsByUserId = await database.CrewMemberStatistics
            .Where(statistics => participatingUserIds.Contains(statistics.UserId))
            .ToDictionaryAsync(
                statistics => statistics.UserId,
                StringComparer.Ordinal,
                cancellationToken
            );

        var pairingPlayerIds = doublesPairingGameStatistics
            .SelectMany(
                statistics => new[]
                {
                    statistics.Key.Player1Id,
                    statistics.Key.Player2Id,
                }
            )
            .ToArray();

        var pairingStatisticsByKey = new Dictionary<
            CrewDoublesPairingKey,
            CrewDoublesPairingStatistics>();

        if (doublesPairingGameStatistics.Length > 0)
        {
            pairingStatisticsByKey = await database.CrewDoublesPairingStatistics
                .Where(statistics =>
                    pairingPlayerIds.Contains(statistics.Player1Id)
                    && pairingPlayerIds.Contains(statistics.Player2Id)
                )
                .ToDictionaryAsync(
                    statistics => CrewDoublesPairingKey.Create(
                        statistics.Player1Id,
                        statistics.Player2Id
                    ),
                    cancellationToken
                );
        }

        var matchupUserIds = matchupGameStatistics
            .SelectMany(statistics => new[]
            {
                statistics.Key.UserId,
                statistics.Key.OpponentUserId,
            })
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var matchupStatisticsByKey = new Dictionary<
            CrewMatchupKey,
            CrewMatchupStatistics>();

        if (matchupGameStatistics.Length > 0)
        {
            matchupStatisticsByKey = await database.CrewMatchupStatistics
                .Where(statistics =>
                    matchupUserIds.Contains(statistics.UserId)
                    && matchupUserIds.Contains(statistics.OpponentUserId)
                )
                .ToDictionaryAsync(
                    statistics => new CrewMatchupKey(
                        statistics.UserId,
                        statistics.OpponentUserId
                    ),
                    cancellationToken
                );
        }

        var createdStatistics = new List<CrewMemberStatistics>();
        var updatedStatistics = new List<CrewMemberStatistics>();
        var createdPairingStatistics = new List<CrewDoublesPairingStatistics>();
        var updatedPairingStatistics = new List<CrewDoublesPairingStatistics>();
        var createdMatchupStatistics = new List<CrewMatchupStatistics>();
        var updatedMatchupStatistics = new List<CrewMatchupStatistics>();

        foreach (var playerGameStats in playerGameStatistics)
        {
            if (!statisticsByUserId.TryGetValue(playerGameStats.UserId, out var statistics))
            {
                statistics = new CrewMemberStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = playerGameStats.UserId,
                };
                statisticsByUserId.Add(playerGameStats.UserId, statistics);
                createdStatistics.Add(statistics);
            }
            else
            {
                updatedStatistics.Add(statistics);
            }

            statistics.PointsScored += playerGameStats.PointsScored;
            statistics.PointsConceded += playerGameStats.PointsConceded;

            if (playerGameStats.IsThatsAPaddlin)
            {
                statistics.ThatsAPaddlin += 1;
            }

            if (playerGameStats.Won)
            {
                statistics.Wins += 1;

                // Starting a new win streak
                if (statistics.CurrentWinStreak == 0)
                {
                    statistics.CurrentWinStreakStartedAtUtc = game.PlayedAtUtc;
                }

                statistics.CurrentWinStreak += 1;

                // Started a new longest win streak
                if (statistics.CurrentWinStreak > statistics.LongestWinStreak)
                {
                    statistics.LongestWinStreak = statistics.CurrentWinStreak;
                    statistics.LongestWinStreakStartedAtUtc = statistics.CurrentWinStreakStartedAtUtc;
                    statistics.LongestWinStreakEndedAtUtc = null;
                }
            }
            else
            {
                statistics.Losses += 1;

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
        }

        foreach (var doublesPairingGameStats in doublesPairingGameStatistics)
        {
            if (!pairingStatisticsByKey.TryGetValue(doublesPairingGameStats.Key, out var statistics))
            {
                statistics = new CrewDoublesPairingStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    Player1Id = doublesPairingGameStats.Key.Player1Id,
                    Player2Id = doublesPairingGameStats.Key.Player2Id,
                };
                pairingStatisticsByKey.Add(doublesPairingGameStats.Key, statistics);
                createdPairingStatistics.Add(statistics);
            }
            else
            {
                updatedPairingStatistics.Add(statistics);
            }

            statistics.PointsScored += doublesPairingGameStats.PointsScored;
            statistics.PointsConceded += doublesPairingGameStats.PointsConceded;

            if (doublesPairingGameStats.Won)
            {
                statistics.Wins += 1;
            }
            else
            {
                statistics.Losses += 1;
            }
        }

        foreach (var matchupGameStats in matchupGameStatistics)
        {
            if (!matchupStatisticsByKey.TryGetValue(matchupGameStats.Key, out var statistics))
            {
                statistics = new CrewMatchupStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = matchupGameStats.Key.UserId,
                    OpponentUserId = matchupGameStats.Key.OpponentUserId,
                };
                matchupStatisticsByKey.Add(matchupGameStats.Key, statistics);
                createdMatchupStatistics.Add(statistics);
            }
            else
            {
                updatedMatchupStatistics.Add(statistics);
            }

            statistics.PointsScored += matchupGameStats.PointsScored;
            statistics.PointsConceded += matchupGameStats.PointsConceded;

            if (matchupGameStats.Won)
            {
                statistics.Wins += 1;
            }
            else
            {
                statistics.Losses += 1;
            }
        }

        return new CalculateGameStatisticsResult
        {
            Created = [.. createdStatistics],
            Updated = [.. updatedStatistics],
            CreatedPairingStatistics = [.. createdPairingStatistics],
            UpdatedPairingStatistics = [.. updatedPairingStatistics],
            CreatedMatchupStatistics = [.. createdMatchupStatistics],
            UpdatedMatchupStatistics = [.. updatedMatchupStatistics],
        };
    }
}

internal sealed record CalculateGameStatisticsResult
{
    public required CrewMemberStatistics[] Created { get; init; }

    public required CrewMemberStatistics[] Updated { get; init; }

    public required CrewDoublesPairingStatistics[] CreatedPairingStatistics { get; init; }

    public required CrewDoublesPairingStatistics[] UpdatedPairingStatistics { get; init; }

    public required CrewMatchupStatistics[] CreatedMatchupStatistics { get; init; }

    public required CrewMatchupStatistics[] UpdatedMatchupStatistics { get; init; }
}

internal sealed record PlayerGameStatistics
{
    public required string UserId { get; init; }

    public bool Won { get; init; }

    public bool IsThatsAPaddlin { get; init; }

    public long PointsScored { get; init; }

    public long PointsConceded { get; init; }
}

internal sealed record DoublesPairingGameStatistics
{
    public required CrewDoublesPairingKey Key { get; init; }

    public bool Won { get; init; }

    public long PointsScored { get; init; }

    public long PointsConceded { get; init; }
}

internal sealed record MatchupGameStatistics
{
    public required CrewMatchupKey Key { get; init; }

    public bool Won { get; init; }

    public long PointsScored { get; init; }

    public long PointsConceded { get; init; }
}
