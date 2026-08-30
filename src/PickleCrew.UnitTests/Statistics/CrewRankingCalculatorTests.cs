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
            CreateStatistics("user_casey", crewScore: 110),
            CreateStatistics("user_blair", crewScore: 305),
            CreateStatistics("user_zero", crewScore: 0),
            CreateStatistics("user_alex", crewScore: 305),
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
    public void Rank_WhenCrewScoresDiffer_ShouldRankByCrewScore()
    {
        var statistics = new[]
        {
            CreateStatistics("user_lower_score", crewScore: 320),
            CreateStatistics("user_higher_score", crewScore: 325),
        };

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        Assert.Collection(
            rankedStatistics,
            memberStatistics => AssertRanking(memberStatistics, "user_higher_score", 1),
            memberStatistics => AssertRanking(memberStatistics, "user_lower_score", 2)
        );
    }

    [Fact]
    public void Rank_WhenScoresAreEqual_ShouldOrderByUserIdAndAssignFirstRank()
    {
        var statistics = new[]
        {
            CreateStatistics("user_2", crewScore: 0),
            CreateStatistics("user_3", crewScore: 0),
            CreateStatistics("user_1", crewScore: 0),
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
        long crewScore)
    {
        return new CrewMemberStatisticsTotals
        {
            UserId = userId,
            Wins = 0,
            Losses = 0,
            GamesPlayed = 0,
            WinPercentage = 0,
            ThatsAPaddlin = 0,
            CurrentWinStreak = 0,
            LongestWinStreak = 0,
            PointsScored = 0,
            PointsConceded = 0,
            PointDifferential = 0,
            CrewScore = crewScore,
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
