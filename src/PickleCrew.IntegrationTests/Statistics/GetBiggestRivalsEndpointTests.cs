using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetBiggestRivalsEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetBiggestRivals_WhenCrewHasNoMatchups_ShouldReturnNullRivalry()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBiggestRivalsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Rivalry);
    }

    [Fact]
    public async Task GetBiggestRivals_WhenMatchupHasFewerThanThreeGames_ShouldReturnNullRivalry()
    {
        const string administratorId = "user_rivals_minimum_admin";
        const string memberId = "user_rivals_minimum_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGames(
                side1: administratorId,
                side2: memberId,
                scores:
                [
                    (11, 8),
                    (9, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBiggestRivalsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Rivalry);
    }

    [Fact]
    public async Task GetBiggestRivals_WhenMatchupsDifferInBalanceAndGamesPlayed_ShouldReturnMostPlayedClosestMatchup()
    {
        const string administratorId = "user_rivals_admin";
        const string firstRivalId = "user_rivals_alpha";
        const string secondRivalId = "user_rivals_beta";
        const string lessBalancedId = "user_rivals_gamma";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId, displayName: "Administrator")
            .WithMember(
                id: firstRivalId,
                displayName: "Alpha Rival",
                profilePictureUrl: "https://example.test/alpha.jpg"
            )
            .WithMember(
                id: secondRivalId,
                displayName: "Beta Rival",
                profilePictureUrl: "https://example.test/beta.jpg"
            )
            .WithMember(id: lessBalancedId, displayName: "Gamma Rival")
            .WithSinglesGames(
                side1: administratorId,
                side2: lessBalancedId,
                scores:
                [
                    (11, 4),
                    (11, 5),
                    (11, 6),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: firstRivalId,
                scores:
                [
                    (11, 8),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: firstRivalId,
                side2: secondRivalId,
                scores:
                [
                    (11, 4),
                    (5, 11),
                    (11, 6),
                    (7, 11),
                    (11, 8),
                    (9, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBiggestRivalsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var rivalry = Assert.IsType<BiggestRivalryHttpResponse>(result?.Rivalry);
        Assert.Equal(6, rivalry.GamesPlayed);
        Assert.Collection(
            rivalry.Players,
            player =>
            {
                Assert.Equal(firstRivalId, player.UserId);
                Assert.Equal("Alpha Rival", player.DisplayName);
                Assert.Equal("https://example.test/alpha.jpg", player.ProfilePictureUrl);
                Assert.Equal(3, player.Wins);
                Assert.Equal(3, player.Losses);
                Assert.Equal(0.5m, player.WinPercentage);
            },
            player =>
            {
                Assert.Equal(secondRivalId, player.UserId);
                Assert.Equal("Beta Rival", player.DisplayName);
                Assert.Equal("https://example.test/beta.jpg", player.ProfilePictureUrl);
                Assert.Equal(3, player.Wins);
                Assert.Equal(3, player.Losses);
                Assert.Equal(0.5m, player.WinPercentage);
            }
        );
    }

    [Fact]
    public async Task GetBiggestRivals_WhenBalancedMatchupContainsInactiveMember_ShouldReturnActiveMatchupAndPreserveHistory()
    {
        const string administratorId = "user_rivals_active_admin";
        const string activeMemberId = "user_rivals_active_member";
        const string inactiveMemberId = "user_rivals_inactive_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId, displayName: "Active Admin")
            .WithMember(id: activeMemberId, displayName: "Active Member")
            .WithMember(
                id: inactiveMemberId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Inactive Rival"
            )
            .BuildAsync();
        await SeedMatchupAsync(
            context,
            administratorId,
            activeMemberId,
            firstPlayerWins: 2,
            secondPlayerWins: 1
        );
        await SeedMatchupAsync(
            context,
            administratorId,
            inactiveMemberId,
            firstPlayerWins: 5,
            secondPlayerWins: 5
        );
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Rivals Crew")
            .BuildAsync();
        await SeedMatchupAsync(
            otherCrew,
            administratorId,
            activeMemberId,
            firstPlayerWins: 20,
            secondPlayerWins: 20
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBiggestRivalsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var rivalry = Assert.IsType<BiggestRivalryHttpResponse>(result?.Rivalry);
        Assert.Equal(
            [administratorId, activeMemberId],
            rivalry.Players.Select(player => player.UserId).Order().ToArray()
        );
        Assert.Equal(3, rivalry.GamesPlayed);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.CrewMatchupStatistics.AnyAsync(matchup =>
            matchup.CrewId == context.Id
            && (matchup.UserId == inactiveMemberId
                || matchup.OpponentUserId == inactiveMemberId)
        ));
    }

    [Fact]
    public async Task GetBiggestRivals_WhenEligibleMatchupsRemainTied_ShouldUsePlayerIdentifiers()
    {
        const string administratorId = "user_rivals_tie_admin";
        const string alphaId = "user_rivals_tie_alpha";
        const string betaId = "user_rivals_tie_beta";
        const string gammaId = "user_rivals_tie_gamma";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: alphaId)
            .WithMember(id: betaId)
            .WithMember(id: gammaId)
            .BuildAsync();
        await SeedMatchupAsync(
            context,
            betaId,
            gammaId,
            firstPlayerWins: 2,
            secondPlayerWins: 2
        );
        await SeedMatchupAsync(
            context,
            administratorId,
            alphaId,
            firstPlayerWins: 2,
            secondPlayerWins: 2
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBiggestRivalsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var rivalry = Assert.IsType<BiggestRivalryHttpResponse>(result?.Rivalry);
        Assert.Equal(
            [administratorId, alphaId],
            rivalry.Players.Select(player => player.UserId).Order().ToArray()
        );
    }

    [Fact]
    public async Task GetBiggestRivals_WhenMembershipProviderFails_ShouldReturnProblemDetails()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailOperationAsync(
            scenarioId,
            FakeWorkOsOperation.ListActiveMembers
        );
        using var apiClient = fixture.CreateApiClient(
            context.Administrator,
            scenarioId
        );

        using var response = await apiClient.GetAsync("/statistics/biggest-rivals");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
    }

    private async Task SeedMatchupAsync(
        BuiltCrewTestData context,
        string firstPlayerId,
        string secondPlayerId,
        long firstPlayerWins,
        long secondPlayerWins)
    {
        await using var database = fixture.CreateDbContext();
        database.CrewMatchupStatistics.AddRange(
            new CrewMatchupStatistics
            {
                CrewId = context.Id,
                CrewUuid = context.Uuid,
                UserId = firstPlayerId,
                OpponentUserId = secondPlayerId,
                Wins = firstPlayerWins,
                Losses = secondPlayerWins,
            },
            new CrewMatchupStatistics
            {
                CrewId = context.Id,
                CrewUuid = context.Uuid,
                UserId = secondPlayerId,
                OpponentUserId = firstPlayerId,
                Wins = secondPlayerWins,
                Losses = firstPlayerWins,
            }
        );
        await database.SaveChangesAsync();
    }
}
