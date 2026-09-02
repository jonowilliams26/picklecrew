using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
            .WithSinglesGames(
                side1: lowerUserId,
                side2: "Selected Holder Opponent",
                scores:
                [
                    (8, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: higherUserId,
                side2: "Higher Identifier Opponent",
                scores:
                [
                    (8, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: betterDifferentialId,
                side2: "Better Differential Opponent",
                scores:
                [
                    (9, 11),
                    (9, 11),
                    (8, 11),
                    (8, 11),
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
            .WithSinglesGames(
                side1: activeHolderId,
                side2: "Active Holder Opponent",
                scores:
                [
                    (5, 11),
                    (5, 11),
                ]
            )
            .WithSinglesGames(
                side1: inactiveHolderId,
                side2: "Former Holder Opponent",
                scores:
                [
                    (0, 11),
                    (0, 11),
                    (0, 11),
                ]
            )
            .BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithName("Other Limp Wrist Crew")
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithSinglesGames(
                side1: otherCrewAdministratorId,
                side2: "Other Crew Opponent",
                scores:
                [
                    (0, 11),
                    (0, 11),
                    (0, 11),
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
        Assert.Equal(activeHolderId, holder.UserId);
        Assert.Equal("Active Holder", holder.DisplayName);
        Assert.Equal(2, holder.Losses);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalLosses = await verificationDatabase.PlayerStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactiveHolderId
            )
            .SumAsync(statistics => statistics.Losses);
        Assert.Equal(3, historicalLosses);
    }
}
