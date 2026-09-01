using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetLimpWristSyndromeEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetLimpWristSyndrome_WhenActiveMemberHasMostLosses_ShouldReturnHolder()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var holderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: holderId,
                displayName: "Casey Backhand",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: holderId,
                scores:
                [
                    (11, 7),
                    (11, 4),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/limp-wrist-syndrome"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetLimpWristSyndromeHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var holder = Assert.IsType<LimpWristSyndromeHttpResponse>(
            result?.LimpWristSyndrome
        );
        Assert.Equal(holderId, holder.UserId);
        Assert.Equal("Casey Backhand", holder.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", holder.ProfilePictureUrl);
        Assert.Equal(2, holder.Losses);
        Assert.Equal(-11, holder.PointDifferential);
    }

    [Fact]
    public async Task GetLimpWristSyndrome_WhenLossesAreTied_ShouldUseLowestPointDifferentialThenUserId()
    {
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var betterDifferentialId = $"user_test_000_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: lowerUserId, displayName: "Selected Holder")
            .WithMember(id: higherUserId, displayName: "Higher Identifier")
            .WithMember(id: betterDifferentialId, displayName: "Better Differential")
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = lowerUserId,
                    Losses = 4,
                    PointsScored = 20,
                    PointsConceded = 35,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = higherUserId,
                    Losses = 4,
                    PointsScored = 18,
                    PointsConceded = 33,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = betterDifferentialId,
                    Losses = 4,
                    PointsScored = 24,
                    PointsConceded = 34,
                }
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync(
            "/statistics/limp-wrist-syndrome"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetLimpWristSyndromeHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var holder = Assert.IsType<LimpWristSyndromeHttpResponse>(
            result?.LimpWristSyndrome
        );
        Assert.Equal(lowerUserId, holder.UserId);
        Assert.Equal("Selected Holder", holder.DisplayName);
        Assert.Equal(4, holder.Losses);
        Assert.Equal(-15, holder.PointDifferential);
    }

    [Fact]
    public async Task GetLimpWristSyndrome_WhenEveryActiveMemberHasZeroLosses_ShouldReturnNoHolder()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync(
            "/statistics/limp-wrist-syndrome"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetLimpWristSyndromeHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.LimpWristSyndrome);
    }

    [Fact]
    public async Task GetLimpWristSyndrome_WhenFormerAndOtherCrewMembersHaveMoreLosses_ShouldReturnActiveCrewHolder()
    {
        var activeHolderId = $"user_test_{Guid.NewGuid():N}";
        var inactiveHolderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activeHolderId, displayName: "Active Holder")
            .WithMember(
                id: inactiveHolderId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Holder"
            )
            .BuildAsync();
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Limp Wrist Crew")
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = activeHolderId,
                    Losses = 2,
                    PointsScored = 10,
                    PointsConceded = 22,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = inactiveHolderId,
                    Losses = 100,
                },
                new CrewMemberStatistics
                {
                    CrewId = otherCrew.Id,
                    CrewUuid = otherCrew.Uuid,
                    UserId = otherCrew.Administrator.UserId,
                    Losses = 200,
                }
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync(
            "/statistics/limp-wrist-syndrome"
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetLimpWristSyndromeHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var holder = Assert.IsType<LimpWristSyndromeHttpResponse>(
            result?.LimpWristSyndrome
        );
        Assert.Equal(activeHolderId, holder.UserId);
        Assert.Equal("Active Holder", holder.DisplayName);
        Assert.Equal(2, holder.Losses);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalLosses = await verificationDatabase.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveHolderId
            )
            .Select(statistics => statistics.Losses)
            .SingleAsync();
        Assert.Equal(100, historicalLosses);
    }
}
