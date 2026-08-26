using PickleCrew.Api.Statistics.Services;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class CrewRankingCalculatorTests
{
    [Fact]
    public void Rank_WhenScoresIncludeTie_ShouldAssignCompetitionRanks()
    {
        var statistics = new[]
        {
            CreateStatistics("user_casey", 1),
            CreateStatistics("user_blair", 3),
            CreateStatistics("user_zero", 0),
            CreateStatistics("user_alex", 3),
        };

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        Assert.Collection(
            rankedStatistics,
            memberStatistics => AssertRanking(memberStatistics, "user_alex", 1),
            memberStatistics => AssertRanking(memberStatistics, "user_blair", 1),
            memberStatistics => AssertRanking(memberStatistics, "user_casey", 3),
            memberStatistics => AssertRanking(memberStatistics, "user_zero", 4)
        );
    }

    [Fact]
    public void Rank_WhenScoresAreEqual_ShouldOrderByUserIdAndAssignFirstRank()
    {
        var statistics = new[]
        {
            CreateStatistics("user_2", 0),
            CreateStatistics("user_3", 0),
            CreateStatistics("user_1", 0),
        };

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        Assert.Equal(
            ["user_1", "user_2", "user_3"],
            rankedStatistics.Select(memberStatistics => memberStatistics.UserId)
        );
        Assert.All(
            rankedStatistics,
            memberStatistics => Assert.Equal(1, memberStatistics.Rank)
        );
    }

    private static CrewMemberStatisticsTotals CreateStatistics(
        string userId,
        long wins)
    {
        return new CrewMemberStatisticsTotals
        {
            UserId = userId,
            Wins = wins,
            Losses = 0,
            CurrentWinStreak = 0,
            LongestWinStreak = 0,
            PointsScored = 0,
            PointsConceded = 0,
        };
    }

    private static void AssertRanking(
        CrewMemberStatisticsTotals statistics,
        string userId,
        int rank)
    {
        Assert.Equal(userId, statistics.UserId);
        Assert.Equal(rank, statistics.Rank);
    }
}
