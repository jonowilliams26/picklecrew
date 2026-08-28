using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
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
        Assert.NotNull(result);
        Assert.Equal(leaderId, result.UserId);
        Assert.Equal("Casey Paddler", result.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", result.ProfilePictureUrl);
        Assert.Equal(2, result.ThatsAPaddlin);
    }

    [Fact]
    public async Task GetChiefPaddler_WhenLeadersAreTied_ShouldReturnAlphabeticallyFirstMember()
    {
        var alphaId = $"user_test_zulu_{Guid.NewGuid():N}";
        var zuluId = $"user_test_alpha_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: zuluId, displayName: "Zulu Leader")
            .WithMember(id: alphaId, displayName: "Alpha Leader")
            .WithSinglesGame(
                side1: (zuluId, 11),
                side2: ("Zulu Guest", 0)
            )
            .WithSinglesGame(
                side1: (alphaId, 11),
                side2: ("Alpha Guest", 0)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(alphaId, result.UserId);
        Assert.Equal("Alpha Leader", result.DisplayName);
        Assert.Equal(1, result.ThatsAPaddlin);
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
        Assert.Null(result.UserId);
        Assert.Null(result.DisplayName);
        Assert.Null(result.ProfilePictureUrl);
        Assert.Equal(0, result.ThatsAPaddlin);
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
            .BuildAsync();
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Chief Paddler Crew")
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = inactiveLeaderId,
                    ThatsAPaddlin = 100,
                },
                new CrewMemberStatistics
                {
                    CrewId = otherCrew.Id,
                    CrewUuid = otherCrew.Uuid,
                    UserId = otherCrew.Administrator.UserId,
                    ThatsAPaddlin = 200,
                }
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/chief-paddler");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetChiefPaddlerHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(activeLeaderId, result.UserId);
        Assert.Equal("Active Leader", result.DisplayName);
        Assert.Equal(2, result.ThatsAPaddlin);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalCount = await verificationDatabase.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveLeaderId
            )
            .Select(statistics => statistics.ThatsAPaddlin)
            .SingleAsync();
        Assert.Equal(100, historicalCount);
    }
}
