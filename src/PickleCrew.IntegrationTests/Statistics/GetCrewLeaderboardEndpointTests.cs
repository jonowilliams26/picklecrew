using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewLeaderboardEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewLeaderboard_WhenMembersHaveDifferentScoresAndFormerMembers_ShouldRankOnlyActiveMembers()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var winnerId = $"user_test_{Guid.NewGuid():N}";
        var tiedId = $"user_test_{Guid.NewGuid():N}";
        var formerId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(
                id: winnerId,
                displayName: "A Winner",
                profilePictureUrl: "https://example.test/winner.jpg"
            )
            .WithMember(id: tiedId, displayName: "B Tied")
            .WithMember(displayName: "Zero Games")
            .WithMember(
                id: formerId,
                displayName: "Former Champion",
                status: FakeMembershipStatus.Inactive
            )
            .WithSinglesGames(
                side1: winnerId,
                side2: "Winner Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (7, 11),
                ]
            )
            .WithSinglesGames(
                side1: tiedId,
                side2: "Tied Opponent",
                scores:
                [
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (7, 11),
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
                    (7, 11),
                    (7, 11),
                    (7, 11),
                    (7, 11),
                ]
            )
            .BuildAsync();
        var winner = context.Members[0];
        var tied = context.Members[1];
        var zeroGameMember = context.Members[2];
        var former = context.Members[3];

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/leaderboard");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLeaderboardHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Collection(
            result.Entries,
            entry => AssertEntry(entry, winner.UserId, 1, 5, 1, 516),
            entry => AssertEntry(entry, tied.UserId, 2, 3, 2, 304),
            entry => AssertEntry(
                entry,
                context.Administrator.UserId,
                3,
                3,
                4,
                296
            ),
            entry => AssertEntry(entry, zeroGameMember.UserId, 4, 0, 0, 0)
        );
        Assert.Equal("https://example.test/winner.jpg", result.Entries[0].ProfilePictureUrl);
        Assert.DoesNotContain(result.Entries, entry => entry.UserId == former.UserId);
    }

    [Fact]
    public async Task GetCrewLeaderboard_WhenCrewHasNoStatistics_ShouldReturnAllActiveMembersTiedAtFirst()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(displayName: "Alpha Player")
            .BuildAsync();
        var member = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/leaderboard");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLeaderboardHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(2, result.Entries.Length);
        Assert.Equal(member.UserId, result.Entries[0].UserId);
        Assert.All(result.Entries, entry =>
        {
            Assert.Equal(1, entry.Rank);
            Assert.Equal(0, entry.Wins);
            Assert.Equal(0, entry.Losses);
            Assert.Equal(0, entry.CrewScore);
        });
    }

    private static void AssertEntry(
        CrewLeaderboardEntryHttpResponse entry,
        string userId,
        int rank,
        long wins,
        long losses,
        long crewScore)
    {
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(rank, entry.Rank);
        Assert.Equal(wins, entry.Wins);
        Assert.Equal(losses, entry.Losses);
        Assert.Equal(crewScore, entry.CrewScore);
    }
}
