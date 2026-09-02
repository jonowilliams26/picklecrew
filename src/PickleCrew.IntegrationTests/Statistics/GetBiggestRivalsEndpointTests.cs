using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
    public async Task GetBiggestRivals_WhenQualifyingDoublesGamesContainGuests_ShouldReturnMemberRivalry()
    {
        const string administratorId = "user_rivals_guest_admin";
        const string memberId = "user_rivals_guest_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId, displayName: "Administrator")
            .WithMember(id: memberId, displayName: "Member Rival")
            .WithDoublesGames(
                side1: (administratorId, "Guest Partner"),
                side2: (memberId, "Guest Opponent"),
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
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
        Assert.Equal(3, rivalry.GamesPlayed);
        Assert.Equal(
            [administratorId, memberId],
            rivalry.Players.Select(player => player.UserId).Order().ToArray()
        );
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

        await using (var database = fixture.CreateDbContext())
        {
            await database.CrewMatchupStatistics
                .Where(statistics => statistics.CrewUuid == context.Uuid)
                .ExecuteDeleteAsync();
        }

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
            .WithSinglesGames(
                side1: administratorId,
                side2: activeMemberId,
                scores:
                [
                    (11, 7),
                    (11, 8),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: inactiveMemberId,
                scores:
                [
                    (11, 4),
                    (5, 11),
                    (11, 6),
                    (7, 11),
                    (11, 8),
                    (9, 11),
                    (11, 7),
                    (8, 11),
                    (11, 9),
                    (6, 11),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithName("Other Rivals Crew")
            .WithMember(id: administratorId)
            .WithMember(id: activeMemberId)
            .WithSinglesGames(
                side1: administratorId,
                side2: activeMemberId,
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
                    (6, 11),
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
        Assert.Equal(
            [administratorId, activeMemberId],
            rivalry.Players.Select(player => player.UserId).Order().ToArray()
        );
        Assert.Equal(3, rivalry.GamesPlayed);
        var administrator = Assert.Single(rivalry.Players, player =>
            player.UserId == administratorId
        );
        Assert.Equal(2, administrator.Wins);
        Assert.Equal(1, administrator.Losses);
        Assert.Equal(2m / 3m, administrator.WinPercentage, precision: 20);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.CrewMatchupGameStatistics.AnyAsync(matchup =>
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
            .WithSinglesGames(
                side1: betaId,
                side2: gammaId,
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
                    (6, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: alphaId,
                scores:
                [
                    (11, 7),
                    (8, 11),
                    (11, 9),
                    (6, 11),
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
}
