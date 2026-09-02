using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetIceManEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetIceMan_WhenSinglesAndDoublesWinsQualify_ShouldReturnActiveLeader()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var leaderId = $"user_test_{Guid.NewGuid():N}";
        var teammateId = $"user_test_{Guid.NewGuid():N}";
        var opponentId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: leaderId,
                displayName: "Casey Clutch",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithMember(id: teammateId)
            .WithMember(id: opponentId)
            .WithSinglesGame(
                side1: (leaderId, 11),
                side2: (administratorId, 9)
            )
            .WithDoublesGame(
                side1: (leaderId, teammateId, 12),
                side2: (administratorId, opponentId, 10)
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (opponentId, 8)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/ice-man");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetIceManHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var iceMan = Assert.IsType<IceManHttpResponse>(result?.IceMan);
        Assert.Equal(leaderId, iceMan.UserId);
        Assert.Equal("Casey Clutch", iceMan.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", iceMan.ProfilePictureUrl);
        Assert.Equal(2, iceMan.ClutchWins);
    }

    [Fact]
    public async Task GetIceMan_WhenClutchWinCountsAreTied_ShouldUseCrewScoreThenUserId()
    {
        var lowerCrewScoreId = $"user_test_middle_{Guid.NewGuid():N}";
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: lowerCrewScoreId, displayName: "Lower Score")
            .WithMember(id: lowerUserId, displayName: "Zulu Master")
            .WithMember(id: higherUserId, displayName: "Alpha Master")
            .WithSinglesGame(
                side1: (lowerCrewScoreId, 11),
                side2: ("Lower Score Opponent", 9)
            )
            .WithSinglesGame(
                side1: (lowerUserId, 11),
                side2: ("Lower Identifier Clutch Opponent", 9)
            )
            .WithSinglesGame(
                side1: (lowerUserId, 11),
                side2: ("Lower Identifier Opponent", 0)
            )
            .WithSinglesGame(
                side1: (higherUserId, 11),
                side2: ("Higher Identifier Clutch Opponent", 9)
            )
            .WithSinglesGame(
                side1: (higherUserId, 11),
                side2: ("Higher Identifier Opponent", 0)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/ice-man");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetIceManHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var iceMan = Assert.IsType<IceManHttpResponse>(result?.IceMan);
        Assert.Equal(lowerUserId, iceMan.UserId);
        Assert.Equal("Zulu Master", iceMan.DisplayName);
        Assert.Equal(1, iceMan.ClutchWins);
    }

    [Fact]
    public async Task GetIceMan_WhenEveryActiveMemberHasZeroClutchWins_ShouldReturnNoHolder()
    {
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (memberId, 11),
                side2: ("Guest Opponent", 8)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/ice-man");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetIceManHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.IceMan);
    }

    [Fact]
    public async Task GetIceMan_WhenFormerAndOtherCrewMembersHaveHigherCounts_ShouldReturnActiveCrewLeaderAndPreserveHistory()
    {
        var activeLeaderId = $"user_test_{Guid.NewGuid():N}";
        var inactiveLeaderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activeLeaderId, displayName: "Active Master")
            .WithMember(
                id: inactiveLeaderId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Master"
            )
            .WithSinglesGames(
                side1: activeLeaderId,
                side2: "Active Opponent",
                scores:
                [
                    (11, 9),
                    (12, 10),
                ]
            )
            .WithSinglesGames(
                side1: inactiveLeaderId,
                side2: "Former Opponent",
                scores:
                [
                    (11, 9),
                    (12, 10),
                    (13, 11),
                ]
            )
            .BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithName("Other Clutch Crew")
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithSinglesGames(
                side1: otherCrewAdministratorId,
                side2: "Other Crew Opponent",
                scores:
                [
                    (11, 9),
                    (12, 10),
                    (13, 11),
                    (14, 12),
                ]
            )
            .BuildAsync();

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/ice-man");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetIceManHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var iceMan = Assert.IsType<IceManHttpResponse>(result?.IceMan);
        Assert.Equal(activeLeaderId, iceMan.UserId);
        Assert.Equal("Active Master", iceMan.DisplayName);
        Assert.Equal(2, iceMan.ClutchWins);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalCount = await verificationDatabase.PlayerStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveLeaderId
                && statistics.Wins > 0
                && statistics.PointsConceded >= 9
            )
            .SumAsync(statistics => statistics.Wins);
        Assert.Equal(3, historicalCount);
    }
}
