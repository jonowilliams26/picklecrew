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
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = lowerCrewScoreId,
                    ThatsAPaddlin = 1,
                    CrewScore = 111,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = lowerUserId,
                    ThatsAPaddlin = 1,
                    CrewScore = 215,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = higherUserId,
                    ThatsAPaddlin = 1,
                    CrewScore = 215,
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
        var chiefPaddler = Assert.IsType<ChiefPaddlerHttpResponse>(result?.ChiefPaddler);
        Assert.Equal(activeLeaderId, chiefPaddler.UserId);
        Assert.Equal("Active Leader", chiefPaddler.DisplayName);
        Assert.Equal(2, chiefPaddler.ThatsAPaddlin);

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
