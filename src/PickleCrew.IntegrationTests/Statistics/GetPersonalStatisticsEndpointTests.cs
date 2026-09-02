using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
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
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var leaderId = $"user_test_{Guid.NewGuid():N}";
        var crewBuilder = fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: leaderId, displayName: "Alex Leader")
            .WithMember(displayName: "Zero Games")
            .WithMember(
                displayName: "Former Leader",
                status: FakeMembershipStatus.Inactive
            )
            .WithSinglesGames(
                side1: administratorId,
                side2: "Administrator Opponent",
                scores:
                [
                    (11, 6),
                    (11, 7),
                    (11, 7),
                    (11, 7),
                    (9, 11),
                    (9, 11),
                ]
            )
            .WithSinglesGames(
                side1: leaderId,
                side2: "Leader Opponent",
                scores:
                [
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 6),
                    (11, 7),
                    (11, 13),
                ]
            );

        var context = await crewBuilder.BuildAsync();
        var otherCrewAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherCrewAdministratorId)
            .WithMember(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Other Crew Opponent", 0)
            )
            .BuildAsync();

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
        Assert.Equal(0, result.ThatsAPaddlin);
        Assert.Equal(0, result.CurrentWinStreak);
        Assert.Null(result.CurrentWinStreakStartedAtUtc);
        Assert.Equal(4, result.LongestWinStreak);
        Assert.Equal(context.Games[0].PlayedAtUtc, result.LongestWinStreakStartedAtUtc);
        Assert.Equal(context.Games[4].PlayedAtUtc, result.LongestWinStreakEndedAtUtc);
        Assert.Equal(4m / 6m, result.WinPercentage, precision: 20);
        Assert.Equal(62, result.PointsScored);
        Assert.Equal(49, result.PointsConceded);
        Assert.Equal(13, result.PointDifferential);
        Assert.Equal(13m / 6m, result.AverageMargin, precision: 20);
        Assert.Equal(417, result.CrewScore);
        Assert.Equal(2, result.CrewRank);
        Assert.Equal(3, result.ActiveCrewMemberCount);
    }

    [Fact]
    public async Task GetPersonalStatistics_WhenGamesIncludePaddlins_ShouldCountQualifyingCrewMemberWins()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var winningTeammateId = $"user_test_{Guid.NewGuid():N}";
        var singlesOpponentId = $"user_test_{Guid.NewGuid():N}";
        var doublesOpponentId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: winningTeammateId)
            .WithMember(id: singlesOpponentId)
            .WithMember(id: doublesOpponentId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (singlesOpponentId, 0)
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (singlesOpponentId, 1)
            )
            .WithSinglesGame(
                side1: (administratorId, 0),
                side2: (singlesOpponentId, 11)
            )
            .WithDoublesGame(
                side1: (administratorId, winningTeammateId, 11),
                side2: (doublesOpponentId, "Guest Player", 0)
            )
            .BuildAsync();

        var administratorStatistics = await GetPersonalStatisticsAsync(
            context.Administrator
        );
        var winningTeammateStatistics = await GetPersonalStatisticsAsync(
            context.Members[0]
        );
        var singlesOpponentStatistics = await GetPersonalStatisticsAsync(
            context.Members[1]
        );
        var doublesOpponentStatistics = await GetPersonalStatisticsAsync(
            context.Members[2]
        );

        Assert.Equal(2, administratorStatistics.ThatsAPaddlin);
        Assert.Equal(1, winningTeammateStatistics.ThatsAPaddlin);
        Assert.Equal(1, singlesOpponentStatistics.ThatsAPaddlin);
        Assert.Equal(0, doublesOpponentStatistics.ThatsAPaddlin);

        await using var database = fixture.CreateDbContext();
        var storedStatistics = await database.PlayerStatistics
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                ThatsAPaddlin = group.Sum(statistics => statistics.ThatsAPaddlin),
            })
            .ToDictionaryAsync(statistics => statistics.UserId);

        Assert.Equal(4, storedStatistics.Count);
        Assert.Equal(2, storedStatistics[administratorId].ThatsAPaddlin);
        Assert.Equal(1, storedStatistics[winningTeammateId].ThatsAPaddlin);
        Assert.Equal(1, storedStatistics[singlesOpponentId].ThatsAPaddlin);
        Assert.Equal(0, storedStatistics[doublesOpponentId].ThatsAPaddlin);
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
        Assert.Equal(0, result.ThatsAPaddlin);
        Assert.Equal(0, result.CurrentWinStreak);
        Assert.Null(result.CurrentWinStreakStartedAtUtc);
        Assert.Equal(0, result.LongestWinStreak);
        Assert.Null(result.LongestWinStreakStartedAtUtc);
        Assert.Null(result.LongestWinStreakEndedAtUtc);
        Assert.Equal(0, result.WinPercentage);
        Assert.Equal(0, result.PointsScored);
        Assert.Equal(0, result.PointsConceded);
        Assert.Equal(0, result.PointDifferential);
        Assert.Equal(0, result.AverageMargin);
        Assert.Equal(0, result.CrewScore);
        Assert.Equal(1, result.CrewRank);
        Assert.Equal(1, result.ActiveCrewMemberCount);
    }

    [Fact]
    public async Task GetPersonalStatistics_WhenPointDifferentialIsNegative_ShouldReturnNegativeAverageMargin()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGames(
                side1: administratorId,
                side2: "Winning Opponent",
                scores:
                [
                    (6, 11),
                    (9, 11),
                ]
            )
            .BuildAsync();

        var statistics = await GetPersonalStatisticsAsync(context.Administrator);

        Assert.Equal(-7, statistics.PointDifferential);
        Assert.Equal(-3.5m, statistics.AverageMargin);
    }

    private async Task<GetPersonalStatisticsHttpResponse> GetPersonalStatisticsAsync(
        TestIdentity identity)
    {
        using var apiClient = fixture.CreateApiClient(identity);
        using var response = await apiClient.GetAsync("/statistics/personal");

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException(
            "The API returned an empty personal statistics response."
        );
    }
}
