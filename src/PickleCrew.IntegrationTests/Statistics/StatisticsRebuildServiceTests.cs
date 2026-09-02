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
        database.CrewMemberStatistics.Add(new CrewMemberStatistics
        {
            CrewId = context.Id,
            CrewUuid = context.Uuid,
            UserId = $"user_without_game_{Guid.NewGuid():N}",
            CrewScore = 999,
        });
        await database.SaveChangesAsync();

        var rebuildService = new StatisticsRebuildService(database, crewUserContext);
        var result = await rebuildService.RebuildAsync(CancellationToken.None);

        Assert.True(result.GamesRecalculated >= 2);

        var memberStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == context.Id)
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
            2,
            await database.CrewDoublesPairingStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            8,
            await database.CrewMatchupStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            8,
            await database.CrewMemberGameStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            4,
            await database.CrewDoublesPairingGameStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            16,
            await database.CrewMatchupGameStatistics.CountAsync(statistics =>
                statistics.CrewId == context.Id
            )
        );
        Assert.Equal(
            4,
            await database.CrewMemberWinStreaks.CountAsync(winStreak =>
                winStreak.CrewId == context.Id
            )
        );
        Assert.Equal(
            await database.CrewMemberGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.CrewMemberGameStatisticsCreated
        );
        Assert.Equal(
            await database.CrewDoublesPairingGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.DoublesPairingGameStatisticsCreated
        );
        Assert.Equal(
            await database.CrewMatchupGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(),
            result.MatchupGameStatisticsCreated
        );
        Assert.Equal(
            await database.CrewMemberWinStreaks
                .IgnoreQueryFilters()
                .CountAsync(),
            result.CrewMemberWinStreaksCreated
        );
    }

    private static void AssertMemberStatistics(
        CrewMemberStatistics statistics,
        long crewScore,
        long pointsScored,
        long pointsConceded)
    {
        Assert.Equal(1, statistics.Wins);
        Assert.Equal(1, statistics.Losses);
        Assert.Equal(crewScore, statistics.CrewScore);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
    }
}
