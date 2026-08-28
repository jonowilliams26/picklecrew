using PickleCrew.Api.Statistics.Services;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class CrewRankingCalculatorTests
{
    [Fact]
    public void Rank_WhenCrewScoresIncludeTie_ShouldAssignCompetitionRanks()
    {
        var statistics = new[]
        {
            CreateStatistics("user_casey", wins: 1, pointDifferential: 10),
            CreateStatistics("user_blair", wins: 3, pointDifferential: 5),
            CreateStatistics("user_zero", wins: 0, pointDifferential: 0),
            CreateStatistics("user_alex", wins: 3, pointDifferential: 5),
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
    public void Rank_WhenPointDifferentialOffsetsWinDifference_ShouldRankByCrewScore()
    {
        var statistics = new[]
        {
            CreateStatistics("user_more_wins", wins: 3, pointDifferential: -60),
            CreateStatistics("user_better_differential", wins: 2, pointDifferential: 60),
        };

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        Assert.Collection(
            rankedStatistics,
            memberStatistics => AssertRanking(memberStatistics, "user_better_differential", 1),
            memberStatistics => AssertRanking(memberStatistics, "user_more_wins", 2)
        );
    }

    [Fact]
    public void Rank_WhenScoresAreEqual_ShouldOrderByUserIdAndAssignFirstRank()
    {
        var statistics = new[]
        {
            CreateStatistics("user_2", wins: 0, pointDifferential: 0),
            CreateStatistics("user_3", wins: 0, pointDifferential: 0),
            CreateStatistics("user_1", wins: 0, pointDifferential: 0),
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
        long wins,
        long pointDifferential)
    {
        return new CrewMemberStatisticsTotals
        {
            UserId = userId,
            Wins = wins,
            Losses = 0,
            GamesPlayed = wins,
            WinPercentage = wins == 0 ? 0 : 1,
            ThatsAPaddlin = 0,
            CurrentWinStreak = 0,
            LongestWinStreak = 0,
            PointsScored = Math.Max(pointDifferential, 0),
            PointsConceded = Math.Max(-pointDifferential, 0),
            PointDifferential = pointDifferential,
            CrewScore = (wins * 100) + pointDifferential,
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
