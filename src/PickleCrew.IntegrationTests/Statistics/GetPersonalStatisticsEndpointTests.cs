using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetPersonalStatisticsEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetPersonalStatistics_WhenStatisticsExist_ShouldReturnDerivedCrewScopedValues()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var leaderId = $"user_test_{Guid.NewGuid():N}";
        var crewBuilder = fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: leaderId, displayName: "Alex Leader")
            .WithMember(displayName: "Zero Games")
            .WithMember(
                displayName: "Former Leader",
                status: FakeMembershipStatus.Inactive
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: "Administrator Opponent",
                scores:
                [
                    (11, 6),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: leaderId,
                side2: "Leader Opponent",
                scores:
                [
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 7),
                    (11, 13),
                ]
            );

        var context = await crewBuilder.BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithMember(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Other Crew Opponent", 0)
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/personal");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(4, result.Wins);
        Assert.Equal(2, result.Losses);
        Assert.Equal(6, result.GamesPlayed);
        Assert.Equal(0, result.CurrentWinStreak);
        Assert.Null(result.CurrentWinStreakStartedAtUtc);
        Assert.Equal(4, result.LongestWinStreak);
        Assert.Equal(context.Games[0].PlayedAtUtc, result.LongestWinStreakStartedAtUtc);
        Assert.Equal(context.Games[4].PlayedAtUtc, result.LongestWinStreakEndedAtUtc);
        Assert.Equal(4m / 6m, result.WinPercentage);
        Assert.Equal(62, result.PointsScored);
        Assert.Equal(49, result.PointsConceded);
        Assert.Equal(13, result.PointDifferential);
        Assert.Equal(400, result.CrewScore);
        Assert.Equal(2, result.CrewRank);
        Assert.Equal(3, result.ActiveCrewMemberCount);
    }

    [Fact]
    public async Task GetPersonalStatistics_WhenMemberHasNoGames_ShouldReturnZeroValuesAndRank()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/personal");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(0, result.Wins);
        Assert.Equal(0, result.Losses);
        Assert.Equal(0, result.GamesPlayed);
        Assert.Equal(0, result.CurrentWinStreak);
        Assert.Null(result.CurrentWinStreakStartedAtUtc);
        Assert.Equal(0, result.LongestWinStreak);
        Assert.Null(result.LongestWinStreakStartedAtUtc);
        Assert.Null(result.LongestWinStreakEndedAtUtc);
        Assert.Equal(0, result.WinPercentage);
        Assert.Equal(0, result.PointsScored);
        Assert.Equal(0, result.PointsConceded);
        Assert.Equal(0, result.PointDifferential);
        Assert.Equal(0, result.CrewScore);
        Assert.Equal(1, result.CrewRank);
        Assert.Equal(1, result.ActiveCrewMemberCount);
    }
}
