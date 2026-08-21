using PickleCrew.Api.Data;
using PickleCrew.Api.Games.Services;
using Xunit;

namespace PickleCrew.UnitTests.Games;

public sealed class GameStatisticsServiceTests
{
    [Fact]
    public void Calculate_WhenSinglesGameIsComplete_ShouldReturnWinnerAndLoserContributions()
    {
        var game = new Game
        {
            RecorderUserId = "recorder",
            Sides =
            [
                CreateSide(1, 11, "winner"),
                CreateSide(2, 7, "loser"),
            ],
        };

        var contributions = GameStatisticsService.Calculate(game);

        Assert.Collection(
            contributions,
            contribution =>
            {
                Assert.Equal("winner", contribution.UserId);
                Assert.Equal(1, contribution.Wins);
                Assert.Equal(0, contribution.Losses);
                Assert.Equal(11, contribution.PointsScored);
                Assert.Equal(7, contribution.PointsConceded);
            },
            contribution =>
            {
                Assert.Equal("loser", contribution.UserId);
                Assert.Equal(0, contribution.Wins);
                Assert.Equal(1, contribution.Losses);
                Assert.Equal(7, contribution.PointsScored);
                Assert.Equal(11, contribution.PointsConceded);
            }
        );
    }

    private static GameSide CreateSide(int sideNumber, int score, string userId)
    {
        return new GameSide
        {
            SideNumber = sideNumber,
            Score = score,
            Participants =
            [
                new GameParticipant
                {
                    SlotNumber = 1,
                    UserId = userId,
                },
            ],
        };
    }
}
