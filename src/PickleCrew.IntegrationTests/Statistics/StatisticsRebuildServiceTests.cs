using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class StatisticsRebuildServiceTests(AppHostFixture fixture)
{
    [Fact]
    public async Task RebuildAsync_WhenStatisticsExist_ShouldRecalculateAllStatisticsFromGames()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var teammateId = $"user_test_{Guid.NewGuid():N}";
        var firstOpponentId = $"user_test_{Guid.NewGuid():N}";
        var secondOpponentId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: teammateId)
            .WithMember(id: firstOpponentId)
            .WithMember(id: secondOpponentId)
            .WithDoublesGames(
                side1: (administratorId, teammateId),
                side2: (firstOpponentId, secondOpponentId),
                scores: [(11, 7), (9, 11)]
            )
            .BuildAsync();

        var crewUserContext = new CrewUserContext();
        var options = new DbContextOptionsBuilder<CrewScopedDbContext>()
            .UseNpgsql(
                fixture.DatabaseConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
            )
            .Options;
        await using var database = new CrewScopedDbContext(options, crewUserContext);
        await database.PlayerStatistics
            .IgnoreQueryFilters()
            .Where(statistics => statistics.CrewId == context.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(statistics => statistics.CrewScore, 999)
            );

        var rebuildService = new StatisticsRebuildService(database, crewUserContext);
        var result = await rebuildService.RebuildAsync(CancellationToken.None);

        Assert.True(result.GamesRecalculated >= 2);

        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == context.Id)
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new MemberStatisticsTotals
            {
                UserId = group.Key,
                Wins = group.Sum(statistics => statistics.Wins),
                Losses = group.Sum(statistics => statistics.Losses),
                CrewScore = group.Sum(statistics => statistics.CrewScore),
                PointsScored = group.Sum(statistics => statistics.PointsScored),
                PointsConceded = group.Sum(statistics => statistics.PointsConceded),
            })
            .ToDictionaryAsync(statistics => statistics.UserId);

        Assert.Equal(4, memberStatistics.Count);
        AssertMemberStatistics(
            memberStatistics[administratorId],
            crewScore: 104,
            pointsScored: 20,
            pointsConceded: 18
        );
        AssertMemberStatistics(
            memberStatistics[teammateId],
            crewScore: 104,
            pointsScored: 20,
            pointsConceded: 18
        );
        AssertMemberStatistics(
            memberStatistics[firstOpponentId],
            crewScore: 102,
            pointsScored: 18,
            pointsConceded: 20
        );
        AssertMemberStatistics(
            memberStatistics[secondOpponentId],
            crewScore: 102,
            pointsScored: 18,
            pointsConceded: 20
        );
        Assert.Equal(
            8,
            await database.PlayerStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            4,
            await database.DoublesPairingStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            16,
            await database.MatchupStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            4,
            await database.PlayerWinStreaks.CountAsync(winStreak =>
                winStreak.CrewId == context.Id
            )
        );
        Assert.Equal(
            await database.PlayerStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.PlayerStatisticsCreated
        );
        Assert.Equal(
            await database.DoublesPairingStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.DoublesPairingStatisticsCreated
        );
        Assert.Equal(
            await database.MatchupStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.MatchupStatisticsCreated
        );
        Assert.Equal(
            await database.PlayerWinStreaks
                .IgnoreQueryFilters()
                .CountAsync(),
            result.PlayerWinStreaksCreated
        );
    }

    private static void AssertMemberStatistics(
        MemberStatisticsTotals statistics,
        int crewScore,
        int pointsScored,
        int pointsConceded)
    {
        Assert.Equal(1, statistics.Wins);
        Assert.Equal(1, statistics.Losses);
        Assert.Equal(crewScore, statistics.CrewScore);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
    }

    private sealed record MemberStatisticsTotals
    {
        public required string UserId { get; init; }

        public required int Wins { get; init; }

        public required int Losses { get; init; }

        public required int CrewScore { get; init; }

        public required int PointsScored { get; init; }

        public required int PointsConceded { get; init; }
    }
}
