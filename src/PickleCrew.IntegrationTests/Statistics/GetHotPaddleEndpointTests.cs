using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetHotPaddleEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetHotPaddle_WhenActiveMemberHasBestFiveGameForm_ShouldReturnHotPaddle()
    {
        var hotPaddleId = $"user_test_{Guid.NewGuid():N}";
        var challengerId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(
                id: hotPaddleId,
                displayName: "Casey Hot Paddle",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithMember(id: challengerId)
            .WithSinglesGames(
                side1: hotPaddleId,
                side2: "Hot Paddle Opponent",
                scores:
                [
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (11, 7),
                    (11, 8),
                    (9, 11),
                    (12, 10),
                    (11, 5),
                ]
            )
            .WithSinglesGames(
                side1: challengerId,
                side2: "Challenger Opponent",
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
                    (7, 11),
                    (11, 6),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/hot-paddle");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetHotPaddleHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var hotPaddle = Assert.IsType<HotPaddleHttpResponse>(result?.HotPaddle);
        Assert.Equal(hotPaddleId, hotPaddle.UserId);
        Assert.Equal("Casey Hot Paddle", hotPaddle.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", hotPaddle.ProfilePictureUrl);
        Assert.Equal(4, hotPaddle.Wins);
        Assert.Equal(1, hotPaddle.Losses);
        Assert.Equal(5, hotPaddle.GamesPlayed);
        Assert.Equal(0.8m, hotPaddle.WinPercentage);
    }

    [Fact]
    public async Task GetHotPaddle_WhenRecentWinCountsAreTied_ShouldUseHighestPointDifferential()
    {
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: lowerUserId, displayName: "Lower Point Differential")
            .WithMember(id: higherUserId, displayName: "Higher Point Differential")
            .WithSinglesGames(
                side1: lowerUserId,
                side2: "Lower Identifier Opponent",
                scores:
                [
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: higherUserId,
                side2: "Higher Identifier Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (7, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/hot-paddle");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetHotPaddleHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var hotPaddle = Assert.IsType<HotPaddleHttpResponse>(result?.HotPaddle);
        Assert.Equal(higherUserId, hotPaddle.UserId);
        Assert.Equal("Higher Point Differential", hotPaddle.DisplayName);
    }

    [Fact]
    public async Task GetHotPaddle_WhenNoActiveMemberHasFiveGames_ShouldReturnNoHotPaddle()
    {
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: memberId)
            .WithSinglesGames(
                side1: memberId,
                side2: "Opponent",
                scores:
                [
                    (11, 7),
                    (11, 8),
                    (9, 11),
                    (11, 6),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/hot-paddle");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetHotPaddleHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.HotPaddle);
    }

    [Fact]
    public async Task GetHotPaddle_WhenFormerAndOtherCrewMembersHaveBetterForm_ShouldReturnActiveCrewMemberAndPreserveHistory()
    {
        var activeMemberId = $"user_test_{Guid.NewGuid():N}";
        var inactiveMemberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activeMemberId, displayName: "Active Hot Paddle")
            .WithMember(
                id: inactiveMemberId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Hot Paddle"
            )
            .WithSinglesGames(
                side1: activeMemberId,
                side2: "Active Opponent",
                scores:
                [
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: inactiveMemberId,
                side2: "Former Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                ]
            )
            .BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithName("Other Hot Paddle Crew")
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithSinglesGames(
                side1: otherCrewAdministratorId,
                side2: "Other Crew Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/hot-paddle");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetHotPaddleHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var hotPaddle = Assert.IsType<HotPaddleHttpResponse>(result?.HotPaddle);
        Assert.Equal(activeMemberId, hotPaddle.UserId);
        Assert.Equal(0, hotPaddle.Wins);
        Assert.Equal(5, hotPaddle.Losses);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalGameCount = await verificationDatabase.PlayerStatistics
            .AsNoTracking()
            .CountAsync(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveMemberId
            );
        Assert.Equal(5, historicalGameCount);
    }
}
