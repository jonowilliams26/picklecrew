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
                return new UserGameStatistics
                {
                    UserId = x.Participant.UserId!,
                    Won = x.Side.Score > opposingSide.Score,
                    PointsScored = x.Side.Score,
                    PointsConceded = opposingSide.Score,
                };
            })
            .ToArray();

        var participatingUserIds = gameStatistics
            .Select(x => x.UserId)
            .ToArray();

        var statisticsByUserId = await database.CrewMemberStatistics
            .Where(statistics => participatingUserIds.Contains(statistics.UserId))
            .ToDictionaryAsync(
                statistics => statistics.UserId,
                StringComparer.Ordinal,
                cancellationToken
            );

        var createdStatistics = new List<CrewMemberStatistics>();
        var updatedStatistics = new List<CrewMemberStatistics>();

        foreach (var userGameStats in gameStatistics)
        {
            if (!statisticsByUserId.TryGetValue(userGameStats.UserId, out var statistics))
            {
                statistics = new CrewMemberStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = userGameStats.UserId,
                };
                statisticsByUserId.Add(userGameStats.UserId, statistics);
                createdStatistics.Add(statistics);
            }
            else
            {
                updatedStatistics.Add(statistics);
            }

            statistics.PointsScored += userGameStats.PointsScored;
            statistics.PointsConceded += userGameStats.PointsConceded;

            if (userGameStats.Won)
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

        return new CalculateGameStatisticsResult
        {
            Created = [.. createdStatistics],
            Updated = [.. updatedStatistics],
        };
    }
}

internal sealed record CalculateGameStatisticsResult
{
    public required CrewMemberStatistics[] Created { get; init; }

    public required CrewMemberStatistics[] Updated { get; init; }
}

internal sealed record UserGameStatistics
{
    public required string UserId { get; init; }

    public bool Won { get; init; }

    public long PointsScored { get; init; }

    public long PointsConceded { get; init; }
}
