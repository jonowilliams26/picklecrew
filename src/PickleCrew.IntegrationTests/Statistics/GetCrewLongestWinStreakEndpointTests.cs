using System.Net.Http.Json;
using PickleCrew.Api.Data;
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
        var longestWinStreak = Assert.IsType<LongestWinStreakHttpResponse>(
            result?.LongestWinStreak
        );
        Assert.Equal(recordHolderId, longestWinStreak.UserId);
        Assert.Equal("Alex Record", longestWinStreak.DisplayName);
        Assert.Equal("https://example.test/record.jpg", longestWinStreak.ProfilePictureUrl);
        Assert.Equal(4, longestWinStreak.Wins);
        Assert.Equal(context.Games[0].PlayedAtUtc, longestWinStreak.StartedAtUtc);
        Assert.Equal(context.Games[4].PlayedAtUtc, longestWinStreak.EndedAtUtc);
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
        var longestWinStreak = Assert.IsType<LongestWinStreakHttpResponse>(
            result?.LongestWinStreak
        );
        Assert.Equal(recordHolderId, longestWinStreak.UserId);
        Assert.Equal("Active Record", longestWinStreak.DisplayName);
        Assert.Equal(3, longestWinStreak.Wins);
        Assert.Equal(context.Games[0].PlayedAtUtc, longestWinStreak.StartedAtUtc);
        Assert.Null(longestWinStreak.EndedAtUtc);
    }

    [Fact]
    public async Task GetCrewLongestWinStreak_WhenMembersHaveEqualStreaks_ShouldReturnMostRecentRecordHolder()
    {
        var earlierRecordHolderId = $"user_test_{Guid.NewGuid():N}";
        var laterRecordHolderId = $"user_test_{Guid.NewGuid():N}";
        var earlierStartedAtUtc = new DateTimeOffset(
            2026,
            1,
            1,
            12,
            0,
            0,
            TimeSpan.Zero
        );
        var laterStartedAtUtc = earlierStartedAtUtc.AddDays(1);
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: earlierRecordHolderId, displayName: "Earlier Record")
            .WithMember(id: laterRecordHolderId, displayName: "Later Record")
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = earlierRecordHolderId,
                    LongestWinStreak = 3,
                    LongestWinStreakStartedAtUtc = earlierStartedAtUtc,
                },
                new CrewMemberStatistics
                {
                    CrewId = context.Id,
                    CrewUuid = context.Uuid,
                    UserId = laterRecordHolderId,
                    LongestWinStreak = 3,
                    LongestWinStreakStartedAtUtc = laterStartedAtUtc,
                }
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/longest-win-streak");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLongestWinStreakHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        var longestWinStreak = Assert.IsType<LongestWinStreakHttpResponse>(
            result?.LongestWinStreak
        );
        Assert.Equal(laterRecordHolderId, longestWinStreak.UserId);
        Assert.Equal("Later Record", longestWinStreak.DisplayName);
        Assert.Equal(3, longestWinStreak.Wins);
        Assert.Equal(laterStartedAtUtc, longestWinStreak.StartedAtUtc);
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
        Assert.Null(result.LongestWinStreak);
    }
}
