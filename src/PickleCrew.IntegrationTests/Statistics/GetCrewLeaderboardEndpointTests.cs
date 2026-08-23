using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewLeaderboardEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewLeaderboard_WhenMembersIncludeTiesAndFormerMembers_ShouldRankOnlyActiveMembers()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(
                displayName: "A Winner",
                profilePictureUrl: "https://example.test/winner.jpg"
            )
            .WithMember(displayName: "B Tied")
            .WithMember(displayName: "Zero Games")
            .WithMember(
                displayName: "Former Champion",
                status: FakeMembershipStatus.Inactive
            )
            .BuildAsync();
        var winner = context.Members[0];
        var tied = context.Members[1];
        var zeroGameMember = context.Members[2];
        var former = context.Members[3];
        var crewId = context.Id;

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    winner.UserId,
                    5,
                    1,
                    60,
                    40
                ),
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    tied.UserId,
                    3,
                    2,
                    48,
                    45
                ),
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    context.Administrator.UserId,
                    3,
                    4,
                    61,
                    70
                ),
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    former.UserId,
                    100,
                    0,
                    1_100,
                    0
                )
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/leaderboard");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewLeaderboardHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Collection(
            result.Entries,
            entry => AssertEntry(entry, winner.UserId, 1, 5, 1, 500),
            entry => AssertEntry(entry, tied.UserId, 2, 3, 2, 300),
            entry => AssertEntry(
                entry,
                context.Administrator.UserId,
                2,
                3,
                4,
                300
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
