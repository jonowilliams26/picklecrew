using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewLongestWinStreakEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewLongestWinStreak_WhenLongestStreakWasBroken_ShouldReturnHolderAndEndTime()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var recordHolderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: recordHolderId,
                displayName: "Alex Record",
                profilePictureUrl: "https://example.test/record.jpg"
            )
            .WithSinglesGames(
                side1: recordHolderId,
                side2: "Record Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: "Administrator Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/longest-win-streak");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLongestWinStreakHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(recordHolderId, result.UserId);
        Assert.Equal("Alex Record", result.DisplayName);
        Assert.Equal("https://example.test/record.jpg", result.ProfilePictureUrl);
        Assert.Equal(4, result.LongestWinStreak);
        Assert.Equal(context.Games[0].PlayedAtUtc, result.LongestWinStreakStartedAtUtc);
        Assert.Equal(context.Games[4].PlayedAtUtc, result.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task GetCrewLongestWinStreak_WhenLongestStreakIsActive_ShouldReturnStartTimeWithoutEndTime()
    {
        var recordHolderId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: recordHolderId, displayName: "Active Record")
            .WithSinglesGames(
                side1: recordHolderId,
                side2: "Record Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                ]
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/longest-win-streak");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLongestWinStreakHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(recordHolderId, result.UserId);
        Assert.Equal("Active Record", result.DisplayName);
        Assert.Equal(3, result.LongestWinStreak);
        Assert.Equal(context.Games[0].PlayedAtUtc, result.LongestWinStreakStartedAtUtc);
        Assert.Null(result.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task GetCrewLongestWinStreak_WhenCrewHasNoWins_ShouldReturnEmptyRecord()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/longest-win-streak");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLongestWinStreakHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.UserId);
        Assert.Null(result.DisplayName);
        Assert.Null(result.ProfilePictureUrl);
        Assert.Equal(0, result.LongestWinStreak);
        Assert.Null(result.LongestWinStreakStartedAtUtc);
        Assert.Null(result.LongestWinStreakEndedAtUtc);
    }
}
