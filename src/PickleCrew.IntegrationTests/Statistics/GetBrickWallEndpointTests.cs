using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetBrickWallEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetBrickWall_WhenActiveMembersHaveGames_ShouldReturnLowestAveragePointsConceded()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var brickWallId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: brickWallId,
                displayName: "Casey Defender",
                profilePictureUrl: "https://example.test/casey.jpg"
            )
            .WithSinglesGames(
                side1: brickWallId,
                side2: "Brick Wall Opponent",
                scores:
                [
                    (11, 4),
                    (11, 5),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: "Administrator Opponent",
                scores:
                [
                    (11, 6),
                    (11, 7),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/brick-wall");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBrickWallHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var brickWall = Assert.IsType<BrickWallHttpResponse>(result?.BrickWall);
        Assert.Equal(brickWallId, brickWall.UserId);
        Assert.Equal("Casey Defender", brickWall.DisplayName);
        Assert.Equal("https://example.test/casey.jpg", brickWall.ProfilePictureUrl);
        Assert.Equal(2, brickWall.GamesPlayed);
        Assert.Equal(9, brickWall.PointsConceded);
        Assert.Equal(4.5m, brickWall.AveragePointsConceded);
    }

    [Fact]
    public async Task GetBrickWall_WhenAveragesAreTied_ShouldUseUserId()
    {
        var lowerUserId = $"user_test_alpha_{Guid.NewGuid():N}";
        var higherUserId = $"user_test_zulu_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: lowerUserId, displayName: "Selected Defender")
            .WithMember(id: higherUserId, displayName: "Other Defender")
            .WithSinglesGame(
                side1: (lowerUserId, 11),
                side2: ("Selected Defender Opponent", 5)
            )
            .WithSinglesGames(
                side1: higherUserId,
                side2: "Other Defender Opponent",
                scores:
                [
                    (11, 4),
                    (11, 6),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/brick-wall");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBrickWallHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var brickWall = Assert.IsType<BrickWallHttpResponse>(result?.BrickWall);
        Assert.Equal(lowerUserId, brickWall.UserId);
        Assert.Equal(5m, brickWall.AveragePointsConceded);
    }

    [Fact]
    public async Task GetBrickWall_WhenNoActiveMemberHasPlayed_ShouldReturnNoHolder()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/brick-wall");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBrickWallHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.BrickWall);
    }

    [Fact]
    public async Task GetBrickWall_WhenFormerAndOtherCrewMembersHaveLowerAverages_ShouldReturnActiveCrewMember()
    {
        var activeMemberId = $"user_test_{Guid.NewGuid():N}";
        var inactiveMemberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: activeMemberId, displayName: "Active Defender")
            .WithMember(
                id: inactiveMemberId,
                status: FakeMembershipStatus.Inactive,
                displayName: "Former Defender"
            )
            .WithSinglesGame(
                side1: (activeMemberId, 11),
                side2: ("Active Opponent", 6)
            )
            .WithSinglesGame(
                side1: (inactiveMemberId, 11),
                side2: ("Former Opponent", 0)
            )
            .BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithSinglesGame(
                side1: (otherCrewAdministratorId, 11),
                side2: ("Other Crew Opponent", 0)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/brick-wall");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetBrickWallHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var brickWall = Assert.IsType<BrickWallHttpResponse>(result?.BrickWall);
        Assert.Equal(activeMemberId, brickWall.UserId);
        Assert.Equal("Active Defender", brickWall.DisplayName);
        Assert.Equal(6m, brickWall.AveragePointsConceded);
    }
}
