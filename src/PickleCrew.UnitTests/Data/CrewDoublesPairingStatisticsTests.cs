using PickleCrew.Api.Data;
using Xunit;

namespace PickleCrew.UnitTests.Data;

public sealed class CrewDoublesPairingStatisticsTests
{
    [Fact]
    public void CalculatedValues_WhenStatisticsContainGames_ShouldReflectBaseValues()
    {
        var statistics = new CrewDoublesPairingStatistics
        {
            Player1Id = "user_first",
            Player2Id = "user_second",
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
    public void CalculatedValues_WhenStatisticsHaveNoGames_ShouldReturnZeroValues()
    {
        var statistics = new CrewDoublesPairingStatistics
        {
            Player1Id = "user_first",
            Player2Id = "user_second",
        };

        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Equal(0, statistics.WinPercentage);
        Assert.Equal(0, statistics.PointDifferential);
    }
}
