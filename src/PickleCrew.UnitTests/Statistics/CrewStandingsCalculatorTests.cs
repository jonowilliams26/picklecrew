using PickleCrew.Api.Statistics.Services;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class CrewStandingsCalculatorTests
{
    [Fact]
    public void Calculate_WhenMembersHaveStatistics_ShouldDerivePersonalValuesAndCompetitionRanks()
    {
        var activeMembers = new[]
        {
            CreateMember("user_alex", "Alex"),
            CreateMember("user_casey", "Casey"),
            CreateMember("user_blair", "Blair"),
            CreateMember("user_zero", "Zero"),
        };
        var statistics = new[]
        {
            CreateStatistics("user_alex", 3, 1, 44, 30),
            CreateStatistics("user_blair", 3, 2, 53, 48),
            CreateStatistics("user_casey", 1, 3, 35, 42),
        };

        var standings = CrewStandingsCalculator.Calculate(activeMembers, statistics);

        Assert.Collection(
            standings,
            standing => AssertStanding(standing, "user_alex", 1, 3, 1, 300),
            standing => AssertStanding(standing, "user_blair", 1, 3, 2, 300),
            standing => AssertStanding(standing, "user_casey", 3, 1, 3, 100),
            standing => AssertStanding(standing, "user_zero", 4, 0, 0, 0)
        );
        Assert.Equal(4, standings[0].GamesPlayed);
        Assert.Equal(0.75m, standings[0].WinPercentage);
        Assert.Equal(14, standings[0].PointDifferential);
        Assert.Equal(0, standings[3].GamesPlayed);
        Assert.Equal(0, standings[3].WinPercentage);
        Assert.Equal(0, standings[3].PointDifferential);
    }

    [Fact]
    public void Calculate_WhenScoresAndNamesMatch_ShouldOrderByNameCaseInsensitivelyThenUserId()
    {
        var activeMembers = new[]
        {
            CreateMember("user_2", "casey"),
            CreateMember("user_3", "Alex"),
            CreateMember("user_1", "Casey"),
        };

        var standings = CrewStandingsCalculator.Calculate(activeMembers, []);

        Assert.Equal(
            ["user_3", "user_1", "user_2"],
            standings.Select(standing => standing.UserId)
        );
        Assert.All(standings, standing => Assert.Equal(1, standing.Rank));
    }

    [Fact]
    public void Calculate_WhenFormerMemberHasStatistics_ShouldExcludeFormerMember()
    {
        var activeMembers = new[] { CreateMember("user_active", "Active") };
        var statistics = new[]
        {
            CreateStatistics("user_active", 1, 0, 11, 5),
            CreateStatistics("user_former", 100, 0, 1_100, 0),
        };

        var standing = Assert.Single(
            CrewStandingsCalculator.Calculate(activeMembers, statistics)
        );

        Assert.Equal("user_active", standing.UserId);
        Assert.Equal(1, standing.Rank);
    }

    private static ActiveCrewMember CreateMember(string userId, string displayName)
    {
        return new ActiveCrewMember
        {
            WorkOsUserId = userId,
            DisplayName = displayName,
            EmailAddress = $"{userId}@example.test",
            RoleSlug = "member",
        };
    }

    private static CrewMemberStatisticsTotals CreateStatistics(
        string userId,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded)
    {
        return new CrewMemberStatisticsTotals
        {
            UserId = userId,
            Wins = wins,
            Losses = losses,
            PointsScored = pointsScored,
            PointsConceded = pointsConceded,
        };
    }

    private static void AssertStanding(
        CrewStanding standing,
        string userId,
        int rank,
        long wins,
        long losses,
        long crewScore)
    {
        Assert.Equal(userId, standing.UserId);
        Assert.Equal(rank, standing.Rank);
        Assert.Equal(wins, standing.Wins);
        Assert.Equal(losses, standing.Losses);
        Assert.Equal(crewScore, standing.CrewScore);
    }
}
