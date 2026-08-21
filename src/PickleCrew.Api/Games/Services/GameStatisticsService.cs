using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;

namespace PickleCrew.Api.Games.Services;

internal sealed class GameStatisticsService(PickleCrewDbContext database)
{
    public async Task<CrewMemberStatistics[]> GenerateStatistics(
        Game game,
        CancellationToken cancellationToken)
    {
        var contributions = Calculate(game);
        var participatingUserIds = contributions
            .Select(contribution => contribution.UserId)
            .ToArray();
        var statisticsByUserId = await database.CrewMemberStatistics
            .Where(statistics =>
                statistics.CrewId == game.CrewId &&
                participatingUserIds.Contains(statistics.UserId)
            )
            .ToDictionaryAsync(
                statistics => statistics.UserId,
                StringComparer.Ordinal,
                cancellationToken
            );
        var generatedStatistics = new List<CrewMemberStatistics>();

        foreach (var contribution in contributions)
        {
            if (!statisticsByUserId.TryGetValue(contribution.UserId, out var statistics))
            {
                statistics = new CrewMemberStatistics
                {
                    CrewId = game.CrewId,
                    CrewUuid = game.CrewUuid,
                    UserId = contribution.UserId,
                };
                statisticsByUserId.Add(contribution.UserId, statistics);
                generatedStatistics.Add(statistics);
            }

            statistics.Wins += contribution.Wins;
            statistics.Losses += contribution.Losses;
            statistics.PointsScored += contribution.PointsScored;
            statistics.PointsConceded += contribution.PointsConceded;
        }

        return generatedStatistics.ToArray();
    }

    internal static GameStatisticsContribution[] Calculate(Game game)
    {
        var firstSideContributions = CalculateForSide(game.Sides[0], game.Sides[1]);
        var secondSideContributions = CalculateForSide(game.Sides[1], game.Sides[0]);

        return [.. firstSideContributions, .. secondSideContributions];
    }

    private static GameStatisticsContribution[] CalculateForSide(
        GameSide side,
        GameSide opposingSide)
    {
        var won = side.Score > opposingSide.Score;

        return side.Participants
            .Where(participant => participant.UserId is not null)
            .Select(participant => new GameStatisticsContribution
            {
                UserId = participant.UserId!,
                Wins = won ? 1 : 0,
                Losses = won ? 0 : 1,
                PointsScored = side.Score,
                PointsConceded = opposingSide.Score,
            })
            .ToArray();
    }
}

internal sealed record GameStatisticsContribution
{
    public required string UserId { get; init; }

    public long Wins { get; init; }

    public long Losses { get; init; }

    public long PointsScored { get; init; }

    public long PointsConceded { get; init; }
}
