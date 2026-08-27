using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetBestDoublesPairingEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetBestDoublesPairing_WhenCrewHasNoPairingStatistics_ShouldReturnNullPairing()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenAllPairingsHavePlayedFewerThanThreeGames_ShouldReturnNullPairing()
    {
        const string firstMemberId = "user_pairing_minimum_first";
        const string secondMemberId = "user_pairing_minimum_second";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: "user_pairing_minimum_admin")
            .WithMember(id: firstMemberId)
            .WithMember(id: secondMemberId)
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                firstMemberId,
                Wins: 2,
                Losses: 0,
                PointsScored: 22,
                PointsConceded: 10
            ),
            new PairingSeed(
                context.Administrator.UserId,
                secondMemberId,
                Wins: 1,
                Losses: 1,
                PointsScored: 18,
                PointsConceded: 20
            )
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenHighestWinPercentageHasFewerThanThreeGames_ShouldReturnBestQualifiedPairing()
    {
        const string unqualifiedMemberId = "user_pairing_eligibility_unqualified";
        const string qualifiedMemberId = "user_pairing_eligibility_qualified";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: "user_pairing_eligibility_admin",
                displayName: "Eligibility Admin"
            )
            .WithMember(
                id: unqualifiedMemberId,
                displayName: "Unqualified Member"
            )
            .WithMember(
                id: qualifiedMemberId,
                displayName: "Qualified Member"
            )
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                unqualifiedMemberId,
                Wins: 2,
                Losses: 0,
                PointsScored: 22,
                PointsConceded: 10
            ),
            new PairingSeed(
                context.Administrator.UserId,
                qualifiedMemberId,
                Wins: 2,
                Losses: 1,
                PointsScored: 29,
                PointsConceded: 24
            )
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<DoublesPairingHttpResponse>(result?.Pairing);
        Assert.Equal(
            [context.Administrator.UserId, qualifiedMemberId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(3, pairing.GamesPlayed);
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenHistoryContainsOnlySinglesAndMemberGuestSides_ShouldReturnNullPairing()
    {
        const string administratorId = "user_pairing_history_admin";
        const string memberId = "user_pairing_history_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (memberId, 7)
            )
            .WithDoublesGame(
                side1: (administratorId, "History Guest One", 11),
                side2: (memberId, "History Guest Two", 8)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.CrewDoublesPairingStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
        ));
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenHighestPairingContainsInactiveMember_ShouldReturnBestActivePairingAndPreserveHistory()
    {
        const string activeTeammateId = "user_pairing_active_teammate";
        const string firstOpponentId = "user_pairing_active_opponent_one";
        const string secondOpponentId = "user_pairing_active_opponent_two";
        const string inactiveMemberId = "user_pairing_inactive_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: "user_pairing_active_admin",
                displayName: "Active Admin"
            )
            .WithMember(
                id: activeTeammateId,
                displayName: "Active Teammate"
            )
            .WithMember(id: firstOpponentId)
            .WithMember(id: secondOpponentId)
            .WithMember(
                id: inactiveMemberId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Inactive Champion"
            )
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                inactiveMemberId,
                Wins: 20,
                Losses: 0,
                PointsScored: 220,
                PointsConceded: 40
            ),
            new PairingSeed(
                context.Administrator.UserId,
                activeTeammateId,
                Wins: 2,
                Losses: 1,
                PointsScored: 29,
                PointsConceded: 24
            )
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<DoublesPairingHttpResponse>(result?.Pairing);
        Assert.Equal(
            [context.Administrator.UserId, activeTeammateId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(2, pairing.Wins);
        Assert.Equal(1, pairing.Losses);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.CrewDoublesPairingStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
            && (statistics.Player1Id == inactiveMemberId
                || statistics.Player2Id == inactiveMemberId)
        ));
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenPairingsDifferByRankingCriteria_ShouldApplyAllCriteriaWithinActiveCrew()
    {
        const string firstId = "user_pairing_rank_first";
        const string secondId = "user_pairing_rank_second";
        const string thirdId = "user_pairing_rank_third";
        const string winnerId = "user_pairing_rank_winner";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: "user_pairing_rank_admin",
                displayName: "Ranking Admin"
            )
            .WithMember(id: firstId)
            .WithMember(id: secondId)
            .WithMember(id: thirdId)
            .WithMember(id: winnerId, displayName: "Ranking Winner")
            .BuildAsync();
        await SeedPairingsAsync(
            context,
            new PairingSeed(
                context.Administrator.UserId,
                firstId,
                Wins: 3,
                Losses: 1,
                PointsScored: 100,
                PointsConceded: 20
            ),
            new PairingSeed(
                context.Administrator.UserId,
                secondId,
                Wins: 4,
                Losses: 1,
                PointsScored: 50,
                PointsConceded: 49
            ),
            new PairingSeed(
                context.Administrator.UserId,
                thirdId,
                Wins: 8,
                Losses: 2,
                PointsScored: 100,
                PointsConceded: 98
            ),
            new PairingSeed(
                context.Administrator.UserId,
                winnerId,
                Wins: 8,
                Losses: 2,
                PointsScored: 110,
                PointsConceded: 101
            )
        );
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Pairing Crew")
            .BuildAsync();
        await SeedPairingsAsync(
            otherCrew,
            new PairingSeed(
                context.Administrator.UserId,
                winnerId,
                Wins: 100,
                Losses: 0,
                PointsScored: 1100,
                PointsConceded: 0
            )
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<DoublesPairingHttpResponse>(result?.Pairing);
        Assert.Equal(
            [context.Administrator.UserId, winnerId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(8, pairing.Wins);
        Assert.Equal(2, pairing.Losses);
        Assert.Equal(10, pairing.GamesPlayed);
        Assert.Equal(0.8m, pairing.WinPercentage);
        Assert.Equal(110, pairing.PointsScored);
        Assert.Equal(101, pairing.PointsConceded);
        Assert.Equal(9, pairing.PointDifferential);
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenPlayerProfilesChangeAfterRecording_ShouldReturnCurrentProfilesInAlphabeticalOrder()
    {
        const string administratorId = "user_pairing_profile_admin";
        const string teammateId = "user_pairing_profile_teammate";
        const string firstOpponentId = "user_pairing_profile_opponent_one";
        const string secondOpponentId = "user_pairing_profile_opponent_two";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Former Zulu",
                profilePictureUrl: "https://example.test/former-zulu.jpg"
            )
            .WithMember(
                id: teammateId,
                displayName: "Former Alpha",
                profilePictureUrl: "https://example.test/former-alpha.jpg"
            )
            .WithMember(id: firstOpponentId)
            .WithMember(id: secondOpponentId)
            .WithDoublesGames(
                side1: (administratorId, teammateId),
                side2: (firstOpponentId, secondOpponentId),
                (Side1: 11, Side2: 6),
                (Side1: 11, Side2: 7),
                (Side1: 11, Side2: 8)
            )
            .BuildAsync();
        var recordedNames = context.Games
            .SelectMany(game => game.Sides)
            .SelectMany(side => side.Participants)
            .Select(participant => participant.MemberDisplayName)
            .ToArray();
        Assert.Contains("Former Zulu", recordedNames);
        Assert.Contains("Former Alpha", recordedNames);

        using (var fakeWorkOs = fixture.CreateFakeWorkOsClient())
        {
            await fakeWorkOs.UpdateUserAsync(
                administratorId,
                "beta Current",
                "https://example.test/current-beta.jpg"
            );
            await fakeWorkOs.UpdateUserAsync(
                teammateId,
                "Alpha Current",
                "https://example.test/current-alpha.jpg"
            );
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pairing = Assert.IsType<DoublesPairingHttpResponse>(result?.Pairing);
        Assert.Collection(
            pairing.Players,
            player =>
            {
                Assert.Equal(teammateId, player.UserId);
                Assert.Equal("Alpha Current", player.DisplayName);
                Assert.Equal(
                    "https://example.test/current-alpha.jpg",
                    player.ProfilePictureUrl
                );
            },
            player =>
            {
                Assert.Equal(administratorId, player.UserId);
                Assert.Equal("beta Current", player.DisplayName);
                Assert.Equal(
                    "https://example.test/current-beta.jpg",
                    player.ProfilePictureUrl
                );
            }
        );
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenMembershipProviderFails_ShouldReturnProblemDetails()
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

        using var response = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
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
