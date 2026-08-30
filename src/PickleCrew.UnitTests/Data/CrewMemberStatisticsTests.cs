using PickleCrew.Api.Data;
using Xunit;

namespace PickleCrew.UnitTests.Data;

public sealed class CrewMemberStatisticsTests
{
    [Fact]
    public void CalculatedValues_WhenStatisticsContainGames_ShouldReflectBaseValues()
    {
        var statistics = new CrewMemberStatistics
        {
            UserId = "user_test",
            Wins = 2,
            Losses = 1,
            PointsScored = 35,
            PointsConceded = 29,
        };

        Assert.Equal(3, statistics.GamesPlayed);
        Assert.Equal(2m / 3m, statistics.WinPercentage);
        Assert.Equal(6, statistics.PointDifferential);
    }

    [Fact]
    public void CrewScore_WhenAssigned_ShouldReturnAssignedValue()
    {
        var statistics = new CrewMemberStatistics
        {
            UserId = "user_test",
            Wins = 2,
            PointsScored = 35,
            PointsConceded = 29,
            CrewScore = 211,
        };

        Assert.Equal(211, statistics.CrewScore);
    }

    [Fact]
    public void CalculatedValues_WhenStatisticsHaveNoGames_ShouldReturnZeroValues()
    {
        var statistics = new CrewMemberStatistics
        {
            UserId = "user_test",
        };

        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Equal(0, statistics.WinPercentage);
        Assert.Equal(0, statistics.PointDifferential);
        Assert.Equal(0, statistics.CrewScore);
    }
}
