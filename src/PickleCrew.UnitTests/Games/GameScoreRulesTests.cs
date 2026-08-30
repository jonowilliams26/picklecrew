using PickleCrew.Api.Games.RecordGame;
using Xunit;

namespace PickleCrew.UnitTests.Games;

public sealed class GameScoreRulesTests
{
    [Theory]
    [InlineData(11, 0)]
    [InlineData(11, 9)]
    [InlineData(12, 10)]
    [InlineData(15, 13)]
    [InlineData(0, 11)]
    public void IsValidFinalScore_WhenScoreCompletesGame_ShouldReturnTrue(
        int firstScore,
        int secondScore)
    {
        Assert.True(GameScoreRules.IsValidFinalScore(firstScore, secondScore));
    }

    [Theory]
    [InlineData(-1, 11)]
    [InlineData(10, 10)]
    [InlineData(10, 9)]
    [InlineData(11, 10)]
    [InlineData(12, 9)]
    [InlineData(13, 10)]
    [InlineData(14, 11)]
    public void IsValidFinalScore_WhenScoreDoesNotCompleteGame_ShouldReturnFalse(
        int firstScore,
        int secondScore)
    {
        Assert.False(GameScoreRules.IsValidFinalScore(firstScore, secondScore));
    }
}
