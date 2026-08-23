using System.Net.Http.Json;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetPersonalStatisticsEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetPersonalStatistics_WhenStatisticsExist_ShouldReturnDerivedCrewScopedValues()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(displayName: "Alex Leader")
            .WithMember(displayName: "Zero Games")
            .WithMember(
                displayName: "Former Leader",
                status: FakeMembershipStatus.Inactive
            )
            .BuildAsync();
        var otherCrew = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var leader = context.Members[0];
        var former = context.Members[2];
        var crewId = context.Id;
        var otherCrewId = otherCrew.Id;

        await using (var database = fixture.CreateDbContext())
        {
            database.CrewMemberStatistics.AddRange(
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    context.Administrator.UserId,
                    4,
                    2,
                    62,
                    49
                ),
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    leader.UserId,
                    6,
                    1,
                    77,
                    50
                ),
                StatisticsTestData.CreateStatistics(
                    context,
                    crewId,
                    former.UserId,
                    100,
                    0,
                    1_100,
                    0
                ),
                StatisticsTestData.CreateStatistics(
                    otherCrew,
                    otherCrewId,
                    context.Administrator.UserId,
                    50,
                    0,
                    550,
                    0
                )
            );
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var response = await apiClient.GetAsync("/statistics/personal");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(4, result.Wins);
        Assert.Equal(2, result.Losses);
        Assert.Equal(6, result.GamesPlayed);
        Assert.Equal(4m / 6m, result.WinPercentage);
        Assert.Equal(62, result.PointsScored);
        Assert.Equal(49, result.PointsConceded);
        Assert.Equal(13, result.PointDifferential);
        Assert.Equal(400, result.CrewScore);
        Assert.Equal(2, result.CrewRank);
        Assert.Equal(3, result.ActiveCrewMemberCount);
    }

    [Fact]
    public async Task GetPersonalStatistics_WhenMemberHasNoGames_ShouldReturnZeroValuesAndRank()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/statistics/personal");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(0, result.Wins);
        Assert.Equal(0, result.Losses);
        Assert.Equal(0, result.GamesPlayed);
        Assert.Equal(0, result.WinPercentage);
        Assert.Equal(0, result.PointsScored);
        Assert.Equal(0, result.PointsConceded);
        Assert.Equal(0, result.PointDifferential);
        Assert.Equal(0, result.CrewScore);
        Assert.Equal(1, result.CrewRank);
        Assert.Equal(1, result.ActiveCrewMemberCount);
    }
}
