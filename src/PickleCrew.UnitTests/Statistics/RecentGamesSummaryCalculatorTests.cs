using PickleCrew.Api.Statistics;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class RecentGamesSummaryCalculatorTests
{
    [Fact]
    public void Calculate_WhenThereAreNoGames_ShouldReturnZeroValues()
    {
        var summary = RecentGamesSummaryCalculator.Calculate([]);

        Assert.Equal(0, summary.Wins);
        Assert.Equal(0, summary.Losses);
        Assert.Equal(0, summary.WinStreak);
    }

    [Fact]
    public void Calculate_WhenMostRecentGameIsLoss_ShouldReturnZeroWinStreak()
    {
        var summary = RecentGamesSummaryCalculator.Calculate([
            RecentGameResult.Loss,
            RecentGameResult.Win,
            RecentGameResult.Win,
        ]);

        Assert.Equal(2, summary.Wins);
        Assert.Equal(1, summary.Losses);
        Assert.Equal(0, summary.WinStreak);
    }

    [Fact]
    public void Calculate_WhenResultsAreMixed_ShouldStopStreakAtFirstLoss()
    {
        var summary = RecentGamesSummaryCalculator.Calculate([
            RecentGameResult.Win,
            RecentGameResult.Win,
            RecentGameResult.Loss,
            RecentGameResult.Win,
        ]);

        Assert.Equal(3, summary.Wins);
        Assert.Equal(1, summary.Losses);
        Assert.Equal(2, summary.WinStreak);
    }

    [Fact]
    public void Calculate_WhenTenResultsAreWins_ShouldReturnTenGameWinStreak()
    {
        var results = Enumerable.Repeat(RecentGameResult.Win, 10)
            .ToArray();

        var summary = RecentGamesSummaryCalculator.Calculate(results);

        Assert.Equal(10, summary.Wins);
        Assert.Equal(0, summary.Losses);
        Assert.Equal(10, summary.WinStreak);
    }
}
