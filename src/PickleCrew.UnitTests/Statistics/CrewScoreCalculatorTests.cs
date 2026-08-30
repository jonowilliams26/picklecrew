using PickleCrew.Api.Statistics.Services;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class CrewScoreCalculatorTests
{
    [Fact]
    public void Calculate_WhenGameIsWon_ShouldReturnWinPointsPlusPointDifferential()
    {
        var crewScore = CrewScoreCalculator.Calculate(
            won: true,
            pointsScored: 11,
            pointsConceded: 7
        );

        Assert.Equal(104, crewScore);
    }

    [Fact]
    public void Calculate_WhenGameIsLost_ShouldReturnZero()
    {
        var crewScore = CrewScoreCalculator.Calculate(
            won: false,
            pointsScored: 7,
            pointsConceded: 11
        );

        Assert.Equal(0, crewScore);
    }
}
