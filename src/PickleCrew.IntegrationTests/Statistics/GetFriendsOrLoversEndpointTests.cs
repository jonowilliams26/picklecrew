using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetFriendsOrLoversEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetFriendsOrLovers_WhenHistoryContainsOnlySinglesAndGuestPairings_ShouldReturnNullPairing()
    {
        const string administratorId = "user_friends_empty_admin";
        const string memberId = "user_friends_empty_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (memberId, 7)
            )
            .WithDoublesGame(
                side1: (administratorId, "Friendly Guest", 11),
                side2: (memberId, "Loving Guest", 8)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/friends-or-lovers");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFriendsOrLoversHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.CrewDoublesPairingGameStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
        ));
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenPairingHasFewerThanThreeGames_ShouldReturnNullPairing()
    {
        const string administratorId = "user_friends_minimum_admin";
        const string memberId = "user_friends_minimum_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithDoublesGames(
                side1: (administratorId, memberId),
                side2: ("Minimum Guest One", "Minimum Guest Two"),
                scores:
                [
                    (11, 7),
                    (8, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/friends-or-lovers");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFriendsOrLoversHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenMemberPairingPlaysGuestSide_ShouldReturnPairingRecordAndCurrentProfiles()
    {
        const string administratorId = "user_friends_guest_admin";
        const string teammateId = "user_friends_guest_teammate";
        const string opponentId = "user_friends_guest_opponent";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Zulu Friend",
                profilePictureUrl: "https://example.test/zulu.jpg"
            )
            .WithMember(
                id: teammateId,
                displayName: "Alpha Lover",
                profilePictureUrl: "https://example.test/alpha.jpg"
            )
            .WithMember(id: opponentId)
            .WithDoublesGame(
                side1: (administratorId, teammateId, 11),
                side2: (opponentId, "Guest Opponent", 8)
            )
            .WithDoublesGame(
                side1: (administratorId, teammateId, 7),
                side2: (opponentId, "Guest Opponent", 11)
            )
            .WithDoublesGame(
                side1: (administratorId, teammateId, 11),
                side2: (opponentId, "Guest Opponent", 9)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/friends-or-lovers");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFriendsOrLoversHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<FriendsOrLoversPairingHttpResponse>(result?.Pairing);
        Assert.Collection(
            pairing.Players,
            player =>
            {
                Assert.Equal(teammateId, player.UserId);
                Assert.Equal("Alpha Lover", player.DisplayName);
                Assert.Equal("https://example.test/alpha.jpg", player.ProfilePictureUrl);
            },
            player =>
            {
                Assert.Equal(administratorId, player.UserId);
                Assert.Equal("Zulu Friend", player.DisplayName);
                Assert.Equal("https://example.test/zulu.jpg", player.ProfilePictureUrl);
            }
        );
        Assert.Equal(2, pairing.Wins);
        Assert.Equal(1, pairing.Losses);
        Assert.Equal(3, pairing.GamesPlayed);
        Assert.Equal(2m / 3m, pairing.WinPercentage, precision: 20);
        Assert.Equal(1, pairing.PointDifferential);
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenPairingsHaveTiedGameCounts_ShouldOrderByWinsThenPointDifferential()
    {
        const string administratorId = "user_friends_order_admin";
        const string lowerWinsId = "user_friends_order_lower_wins";
        const string lowerDifferentialId = "user_friends_order_lower_differential";
        const string winnerId = "user_friends_order_a_winner";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: lowerWinsId)
            .WithMember(id: lowerDifferentialId)
            .WithMember(id: winnerId)
            .WithDoublesGames(
                side1: (administratorId, lowerWinsId),
                side2: ("Lower Wins Guest One", "Lower Wins Guest Two"),
                scores:
                [
                    (11, 4),
                    (11, 5),
                    (8, 11),
                    (8, 11),
                    (8, 11),
                    (8, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, lowerDifferentialId),
                side2: ("Lower Differential Guest One", "Lower Differential Guest Two"),
                scores:
                [
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, winnerId),
                side2: ("Winner Guest One", "Winner Guest Two"),
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/friends-or-lovers");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFriendsOrLoversHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<FriendsOrLoversPairingHttpResponse>(result?.Pairing);
        Assert.Contains(pairing.Players, player => player.UserId == winnerId);
        Assert.Equal(3, pairing.Wins);
        Assert.Equal(3, pairing.Losses);
        Assert.Equal(6, pairing.GamesPlayed);
        Assert.Equal(6, pairing.PointDifferential);
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenPairingsSpanMembershipsAndCrews_ShouldReturnMostPlayedActiveCrewPairing()
    {
        const string administratorId = "user_friends_scope_admin";
        const string activeWinnerId = "user_friends_scope_winner";
        const string higherWinRateId = "user_friends_scope_higher_win_rate";
        const string inactiveMemberId = "user_friends_scope_inactive";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Scope Admin"
            )
            .WithMember(
                id: activeWinnerId,
                displayName: "Games Winner"
            )
            .WithMember(
                id: higherWinRateId,
                displayName: "Win Rate Winner"
            )
            .WithMember(
                id: inactiveMemberId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Member"
            )
            .WithDoublesGames(
                side1: (administratorId, inactiveMemberId),
                side2: ("Inactive Guest One", "Inactive Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (11, 6),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, higherWinRateId),
                side2: ("Rate Guest One", "Rate Guest Two"),
                scores:
                [
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (11, 6),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, activeWinnerId),
                side2: ("Games Guest One", "Games Guest Two"),
                scores:
                [
                    (11, 8),
                    (11, 9),
                    (8, 11),
                    (8, 11),
                    (8, 11),
                    (8, 11),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithName("Other Friends Crew")
            .WithMember(id: administratorId)
            .WithMember(id: higherWinRateId)
            .WithDoublesGames(
                side1: (administratorId, higherWinRateId),
                side2: ("Other Guest One", "Other Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (11, 6),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/friends-or-lovers");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetFriendsOrLoversHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<FriendsOrLoversPairingHttpResponse>(result?.Pairing);
        Assert.Equal(
            [activeWinnerId, context.Administrator.UserId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(2, pairing.Wins);
        Assert.Equal(4, pairing.Losses);
        Assert.Equal(6, pairing.GamesPlayed);
        Assert.Equal(1m / 3m, pairing.WinPercentage, precision: 20);
        Assert.Equal(-7, pairing.PointDifferential);
    }
}
