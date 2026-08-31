using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
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
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenPairingHasFewerThanThreeGames_ShouldReturnNullPairing()
    {
        const string memberId = "user_friends_minimum_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: "user_friends_minimum_admin")
            .WithMember(id: memberId)
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                memberId,
                Wins: 1,
                Losses: 1,
                PointsScored: 18,
                PointsConceded: 18
            )
        );
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
        Assert.Equal(2m / 3m, pairing.WinPercentage);
        Assert.Equal(1, pairing.PointDifferential);
    }

    [Fact]
    public async Task GetFriendsOrLovers_WhenPairingsHaveTiedGameCounts_ShouldOrderByWinsThenPointDifferential()
    {
        const string lowerWinsId = "user_friends_order_lower_wins";
        const string lowerDifferentialId = "user_friends_order_lower_differential";
        const string winnerId = "user_friends_order_winner";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: "user_friends_order_admin")
            .WithMember(id: lowerWinsId)
            .WithMember(id: lowerDifferentialId)
            .WithMember(id: winnerId)
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                lowerWinsId,
                Wins: 2,
                Losses: 4,
                PointsScored: 80,
                PointsConceded: 50
            ),
            new PairingSeed(
                context.Administrator.UserId,
                lowerDifferentialId,
                Wins: 3,
                Losses: 3,
                PointsScored: 60,
                PointsConceded: 55
            ),
            new PairingSeed(
                context.Administrator.UserId,
                winnerId,
                Wins: 3,
                Losses: 3,
                PointsScored: 61,
                PointsConceded: 55
            )
        );
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
        const string activeWinnerId = "user_friends_scope_winner";
        const string higherWinRateId = "user_friends_scope_higher_win_rate";
        const string inactiveMemberId = "user_friends_scope_inactive";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: "user_friends_scope_admin",
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
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                inactiveMemberId,
                Wins: 100,
                Losses: 0,
                PointsScored: 1100,
                PointsConceded: 0
            ),
            new PairingSeed(
                context.Administrator.UserId,
                higherWinRateId,
                Wins: 5,
                Losses: 0,
                PointsScored: 55,
                PointsConceded: 20
            ),
            new PairingSeed(
                context.Administrator.UserId,
                activeWinnerId,
                Wins: 2,
                Losses: 4,
                PointsScored: 57,
                PointsConceded: 60
            )
        );
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Friends Crew")
            .BuildAsync();
        await SeedPairingsAsync(
            otherCrew,
            new PairingSeed(
                context.Administrator.UserId,
                higherWinRateId,
                Wins: 200,
                Losses: 0,
                PointsScored: 2200,
                PointsConceded: 0
            )
        );
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
        Assert.Equal(1m / 3m, pairing.WinPercentage);
        Assert.Equal(-3, pairing.PointDifferential);
    }

    private async Task SeedPairingsAsync(
        BuiltCrewTestData context,
        params PairingSeed[] pairings)
    {
        await using var database = fixture.CreateDbContext();
        database.CrewDoublesPairingStatistics.AddRange(
            pairings.Select(pairing => CreatePairingStatistics(
                context.Id,
                context.Uuid,
                pairing
            ))
        );
        await database.SaveChangesAsync();
    }

    private static CrewDoublesPairingStatistics CreatePairingStatistics(
        long crewId,
        Guid crewUuid,
        PairingSeed pairing)
    {
        var key = CrewDoublesPairingKey.Create(
            pairing.FirstPlayerId,
            pairing.SecondPlayerId
        );

        return new CrewDoublesPairingStatistics
        {
            CrewId = crewId,
            CrewUuid = crewUuid,
            Player1Id = key.Player1Id,
            Player2Id = key.Player2Id,
            Wins = pairing.Wins,
            Losses = pairing.Losses,
            PointsScored = pairing.PointsScored,
            PointsConceded = pairing.PointsConceded,
        };
    }

    private sealed record PairingSeed(
        string FirstPlayerId,
        string SecondPlayerId,
        long Wins,
        long Losses,
        long PointsScored,
        long PointsConceded);
}
