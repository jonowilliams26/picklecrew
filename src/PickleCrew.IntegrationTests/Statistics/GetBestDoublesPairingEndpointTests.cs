using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
        const string administratorId = "user_pairing_minimum_admin";
        const string firstMemberId = "user_pairing_minimum_first";
        const string secondMemberId = "user_pairing_minimum_second";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: firstMemberId)
            .WithMember(id: secondMemberId)
            .WithDoublesGames(
                side1: (administratorId, firstMemberId),
                side2: (secondMemberId, "Minimum Guest One"),
                scores:
                [
                    (11, 5),
                    (7, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, secondMemberId),
                side2: (firstMemberId, "Minimum Guest Two"),
                scores:
                [
                    (11, 6),
                    (8, 11),
                ]
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
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenHighestWinPercentageHasFewerThanThreeGames_ShouldReturnBestQualifiedPairing()
    {
        const string administratorId = "user_pairing_eligibility_admin";
        const string unqualifiedMemberId = "user_pairing_eligibility_unqualified";
        const string qualifiedMemberId = "user_pairing_eligibility_qualified";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
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
            .WithDoublesGames(
                side1: (administratorId, unqualifiedMemberId),
                side2: (qualifiedMemberId, "Eligibility Guest One"),
                scores:
                [
                    (11, 4),
                    (11, 5),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, qualifiedMemberId),
                side2: (unqualifiedMemberId, "Eligibility Guest Two"),
                scores:
                [
                    (11, 6),
                    (11, 7),
                    (8, 11),
                ]
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
        Assert.False(await database.CrewDoublesPairingGameStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
        ));
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenHighestPairingContainsInactiveMember_ShouldReturnBestActivePairingAndPreserveHistory()
    {
        const string administratorId = "user_pairing_active_admin";
        const string activeTeammateId = "user_pairing_active_teammate";
        const string firstOpponentId = "user_pairing_active_opponent_one";
        const string secondOpponentId = "user_pairing_active_opponent_two";
        const string inactiveMemberId = "user_pairing_inactive_member";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
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
            .WithDoublesGames(
                side1: (administratorId, inactiveMemberId),
                side2: (firstOpponentId, secondOpponentId),
                scores:
                [
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, activeTeammateId),
                side2: (firstOpponentId, secondOpponentId),
                scores:
                [
                    (11, 6),
                    (11, 7),
                    (8, 11),
                ]
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
        var pairing = Assert.IsType<DoublesPairingHttpResponse>(result?.Pairing);
        Assert.Equal(
            [context.Administrator.UserId, activeTeammateId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(2, pairing.Wins);
        Assert.Equal(1, pairing.Losses);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.CrewDoublesPairingGameStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
            && (statistics.Player1Id == inactiveMemberId
                || statistics.Player2Id == inactiveMemberId)
        ));
    }

    [Fact]
    public async Task GetBestDoublesPairing_WhenPairingsDifferByRankingCriteria_ShouldApplyAllCriteriaWithinActiveCrew()
    {
        const string administratorId = "user_pairing_rank_admin";
        const string firstId = "user_pairing_rank_first";
        const string secondId = "user_pairing_rank_second";
        const string thirdId = "user_pairing_rank_third";
        const string winnerId = "user_pairing_rank_winner";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Ranking Admin"
            )
            .WithMember(id: firstId)
            .WithMember(id: secondId)
            .WithMember(id: thirdId)
            .WithMember(id: winnerId, displayName: "Ranking Winner")
            .WithDoublesGames(
                side1: (administratorId, firstId),
                side2: ("First Guest One", "First Guest Two"),
                scores:
                [
                    (11, 5),
                    (11, 6),
                    (11, 7),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, secondId),
                side2: ("Second Guest One", "Second Guest Two"),
                scores:
                [
                    (11, 8),
                    (11, 8),
                    (11, 8),
                    (11, 8),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, thirdId),
                side2: ("Third Guest One", "Third Guest Two"),
                scores:
                [
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (11, 9),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithDoublesGames(
                side1: (administratorId, winnerId),
                side2: ("Winner Guest One", "Winner Guest Two"),
                scores:
                [
                    (11, 1),
                    (11, 2),
                    (11, 3),
                    (11, 4),
                    (11, 5),
                    (11, 6),
                    (11, 7),
                    (11, 8),
                    (9, 11),
                    (8, 11),
                ]
            )
            .BuildAsync();
        await fixture.CreateCrewBuilder()
            .WithName("Other Pairing Crew")
            .WithMember(id: administratorId)
            .WithMember(id: winnerId)
            .WithDoublesGames(
                side1: (administratorId, winnerId),
                side2: ("Other Guest One", "Other Guest Two"),
                scores:
                [
                    (11, 0),
                    (11, 1),
                    (11, 2),
                ]
            )
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            await database.CrewDoublesPairingStatistics
                .Where(statistics => statistics.CrewUuid == context.Uuid)
                .ExecuteDeleteAsync();
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
        Assert.Equal(
            [context.Administrator.UserId, winnerId],
            pairing.Players.Select(player => player.UserId).ToArray()
        );
        Assert.Equal(8, pairing.Wins);
        Assert.Equal(2, pairing.Losses);
        Assert.Equal(10, pairing.GamesPlayed);
        Assert.Equal(0.8m, pairing.WinPercentage);
        Assert.Equal(105, pairing.PointsScored);
        Assert.Equal(58, pairing.PointsConceded);
        Assert.Equal(47, pairing.PointDifferential);
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
                scores:
                [
                    (11, 6),
                    (11, 7),
                    (11, 8),
                ]
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
}
