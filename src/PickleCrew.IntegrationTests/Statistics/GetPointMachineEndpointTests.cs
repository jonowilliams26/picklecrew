using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetPointMachineEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetPointMachine_WhenActiveMemberHasHighestAverage_ShouldReturnPointMachine()
    {
        var pointMachineId = $"user_test_{Guid.NewGuid():N}";
        var challengerId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(
                id: pointMachineId,
                displayName: "Casey Point Machine",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithMember(id: challengerId)
            .WithSinglesGames(
                side1: pointMachineId,
                side2: "Point Machine Opponent",
                scores:
                [
                    (11, 7),
                    (9, 11),
                    (10, 12),
                ]
            )
            .WithSinglesGames(
                side1: challengerId,
                side2: "Challenger Opponent",
                scores:
                [
                    (8, 11),
                    (8, 11),
                    (8, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/point-machine");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPointMachineHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pointMachine = Assert.IsType<PointMachineHttpResponse>(result?.PointMachine);
        Assert.Equal(pointMachineId, pointMachine.UserId);
        Assert.Equal("Casey Point Machine", pointMachine.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", pointMachine.ProfilePictureUrl);
        Assert.Equal(30, pointMachine.PointsScored);
        Assert.Equal(3, pointMachine.GamesPlayed);
        Assert.Equal(10m, pointMachine.AveragePointsScored);
    }

    [Fact]
    public async Task GetPointMachine_WhenAveragesAreTied_ShouldUseMostGamesThenUserId()
    {
        var fewerGamesId = $"user_test_000_{Guid.NewGuid():N}";
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: fewerGamesId, displayName: "Fewer Games")
            .WithMember(id: lowerUserId, displayName: "Selected Point Machine")
            .WithMember(id: higherUserId, displayName: "Higher Identifier")
            .WithSinglesGames(
                side1: fewerGamesId,
                side2: "Fewer Games Opponent",
                scores:
                [
                    (9, 11),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: lowerUserId,
                side2: "Lower Identifier Opponent",
                scores:
                [
                    (9, 11),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: higherUserId,
                side2: "Higher Identifier Opponent",
                scores:
                [
                    (9, 11),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/point-machine");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPointMachineHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pointMachine = Assert.IsType<PointMachineHttpResponse>(result?.PointMachine);
        Assert.Equal(lowerUserId, pointMachine.UserId);
        Assert.Equal("Selected Point Machine", pointMachine.DisplayName);
        Assert.Equal(45, pointMachine.PointsScored);
        Assert.Equal(5, pointMachine.GamesPlayed);
        Assert.Equal(9m, pointMachine.AveragePointsScored);
    }

    [Fact]
    public async Task GetPointMachine_WhenNoActiveMemberHasThreeGames_ShouldReturnNoPointMachine()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGames(
                side1: administratorId,
                side2: "Opponent",
                scores:
                [
                    (11, 7),
                    (11, 8),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/point-machine");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPointMachineHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.PointMachine);
    }

    [Fact]
    public async Task GetPointMachine_WhenFormerAndOtherCrewMembersHaveHigherAverages_ShouldReturnActiveCrewPointMachine()
    {
        var activePointMachineId = $"user_test_{Guid.NewGuid():N}";
        var inactivePointMachineId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activePointMachineId, displayName: "Active Point Machine")
            .WithMember(
                id: inactivePointMachineId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Point Machine"
            )
            .WithSinglesGames(
                side1: activePointMachineId,
                side2: "Active Opponent",
                scores:
                [
                    (9, 11),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: inactivePointMachineId,
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
            .WithName("Other Point Machine Crew")
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
        using var response = await apiClient.GetAsync("/statistics/point-machine");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPointMachineHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var pointMachine = Assert.IsType<PointMachineHttpResponse>(result?.PointMachine);
        Assert.Equal(activePointMachineId, pointMachine.UserId);
        Assert.Equal("Active Point Machine", pointMachine.DisplayName);
        Assert.Equal(9m, pointMachine.AveragePointsScored);

        await using var verificationDatabase = fixture.CreateDbContext();
        var historicalPoints = await verificationDatabase.PlayerStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.CrewId == context.Id
                && statistics.UserId == inactivePointMachineId
            )
            .SumAsync(statistics => statistics.PointsScored);
        Assert.Equal(33, historicalPoints);
    }
}
