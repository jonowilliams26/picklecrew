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

    [Fact]
    public void Calculate_WhenDoublesGameHasFourMembers_ShouldGiveEachMemberTheFullScoreContribution()
    {
        var game = new Game
        {
            RecorderUserId = "recorder",
            Sides =
            [
                CreateSide(1, 11, "winner-one", "winner-two"),
                CreateSide(2, 7, "loser-one", "loser-two"),
            ],
        };

        var contributions = GameStatisticsService.Calculate(game)
            .ToDictionary(contribution => contribution.UserId);

        Assert.Equal(4, contributions.Count);
        AssertContribution(contributions["winner-one"], 1, 0, 11, 7);
        AssertContribution(contributions["winner-two"], 1, 0, 11, 7);
        AssertContribution(contributions["loser-one"], 0, 1, 7, 11);
        AssertContribution(contributions["loser-two"], 0, 1, 7, 11);
    }

    [Fact]
    public void Calculate_WhenDoublesGameIncludesGuests_ShouldReturnOnlyMemberContributions()
    {
        var game = new Game
        {
            RecorderUserId = "recorder",
            Sides =
            [
                CreateSide(1, 13, "winner", null),
                CreateSide(2, 11, "loser", null),
            ],
        };

        var contributions = GameStatisticsService.Calculate(game);

        Assert.Collection(
            contributions,
            contribution =>
            {
                Assert.Equal("winner", contribution.UserId);
                AssertContribution(contribution, 1, 0, 13, 11);
            },
            contribution =>
            {
                Assert.Equal("loser", contribution.UserId);
                AssertContribution(contribution, 0, 1, 11, 13);
            }
        );
    }

    [Fact]
    public void Calculate_WhenSinglesGameIncludesGuest_ShouldReturnOnlyMemberContribution()
    {
        var game = new Game
        {
            RecorderUserId = "recorder",
            Sides =
            [
                CreateSide(1, 11, "winner"),
                CreateSide(2, 9, (string?)null),
            ],
        };

        var contribution = Assert.Single(GameStatisticsService.Calculate(game));

        Assert.Equal("winner", contribution.UserId);
        AssertContribution(contribution, 1, 0, 11, 9);
    }

    private static GameSide CreateSide(
        int sideNumber,
        int score,
        params string?[] userIds)
    {
        return new GameSide
        {
            SideNumber = sideNumber,
            Score = score,
            Participants = userIds
                .Select((userId, index) => new GameParticipant
                {
                    SlotNumber = index + 1,
                    UserId = userId,
                    GuestName = userId is null ? $"Guest {index + 1}" : null,
                })
                .ToList(),
        };
    }

    private static void AssertContribution(
        GameStatisticsContribution contribution,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded)
    {
        Assert.Equal(wins, contribution.Wins);
        Assert.Equal(losses, contribution.Losses);
        Assert.Equal(pointsScored, contribution.PointsScored);
        Assert.Equal(pointsConceded, contribution.PointsConceded);
    }
}
