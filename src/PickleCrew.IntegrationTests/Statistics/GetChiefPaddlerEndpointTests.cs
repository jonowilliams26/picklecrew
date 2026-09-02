using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetChiefPaddlerEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetChiefPaddler_WhenSinglesAndDoublesGamesQualify_ShouldReturnActiveLeader()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var leaderId = $"user_test_{Guid.NewGuid():N}";
        var teammateId = $"user_test_{Guid.NewGuid():N}";
        var opponentId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: leaderId,
                displayName: "Casey Paddler",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithMember(id: teammateId)
            .WithMember(id: opponentId)
            .WithSinglesGame(
                side1: (leaderId, 11),
                side2: (administratorId, 0)
            )
            .WithDoublesGame(
                side1: (leaderId, teammateId, 11),
                side2: (administratorId, opponentId, 0)
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (opponentId, 1)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var chiefPaddler = Assert.IsType<ChiefPaddlerHttpResponse>(result?.ChiefPaddler);
        Assert.Equal(leaderId, chiefPaddler.UserId);
        Assert.Equal("Casey Paddler", chiefPaddler.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", chiefPaddler.ProfilePictureUrl);
        Assert.Equal(2, chiefPaddler.ThatsAPaddlin);
    }

    [Fact]
    public async Task GetChiefPaddler_WhenPaddlinCountsAreTied_ShouldUseCrewScoreThenUserId()
    {
        var lowerCrewScoreId = $"user_test_middle_{Guid.NewGuid():N}";
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: lowerCrewScoreId, displayName: "Lower Score")
            .WithMember(id: lowerUserId, displayName: "Zulu Leader")
            .WithMember(id: higherUserId, displayName: "Alpha Leader")
            .WithSinglesGame(
                side1: (lowerCrewScoreId, 11),
                side2: ("Lower Score Opponent", 0)
            )
            .WithSinglesGame(
                side1: (lowerUserId, 11),
                side2: ("Lower Identifier Paddlin Opponent", 0)
            )
            .WithSinglesGame(
                side1: (lowerUserId, 11),
                side2: ("Lower Identifier Opponent", 7)
            )
            .WithSinglesGame(
                side1: (higherUserId, 11),
                side2: ("Higher Identifier Paddlin Opponent", 0)
            )
            .WithSinglesGame(
                side1: (higherUserId, 11),
                side2: ("Higher Identifier Opponent", 7)
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var chiefPaddler = Assert.IsType<ChiefPaddlerHttpResponse>(result?.ChiefPaddler);
        Assert.Equal(lowerUserId, chiefPaddler.UserId);
        Assert.Equal("Zulu Leader", chiefPaddler.DisplayName);
        Assert.Equal(1, chiefPaddler.ThatsAPaddlin);
    }

    [Fact]
    public async Task GetChiefPaddler_WhenEveryActiveMemberHasZero_ShouldReturnNoHolder()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.ChiefPaddler);
    }

    [Fact]
    public async Task GetChiefPaddler_WhenFormerAndOtherCrewMembersHaveHigherCounts_ShouldReturnActiveCrewLeaderAndPreserveHistory()
    {
        var activeLeaderId = $"user_test_{Guid.NewGuid():N}";
        var inactiveLeaderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activeLeaderId, displayName: "Active Leader")
            .WithMember(
                id: inactiveLeaderId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Leader"
            )
            .WithSinglesGames(
                side1: activeLeaderId,
                side2: "Active Opponent",
                scores:
                [
                    (11, 0),
                    (11, 0),
                ]
            )
            .WithSinglesGames(
                side1: inactiveLeaderId,
                side2: "Former Opponent",
                scores:
                [
                    (11, 0),
                    (11, 0),
                    (11, 0),
                ]
            )
            .BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithName("Other Chief Paddler Crew")
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithSinglesGames(
                side1: otherCrewAdministratorId,
                side2: "Other Crew Opponent",
                scores:
                [
                    (11, 0),
                    (11, 0),
                    (11, 0),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var chiefPaddler = Assert.IsType<ChiefPaddlerHttpResponse>(result?.ChiefPaddler);
        Assert.Equal(activeLeaderId, chiefPaddler.UserId);
        Assert.Equal("Active Leader", chiefPaddler.DisplayName);
        Assert.Equal(2, chiefPaddler.ThatsAPaddlin);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalCount = await verificationDatabase.PlayerStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveLeaderId
            )
            .SumAsync(statistics => statistics.ThatsAPaddlin);
        Assert.Equal(3, historicalCount);
    }
}
