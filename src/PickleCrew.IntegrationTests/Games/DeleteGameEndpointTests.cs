using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Games;
using PickleCrew.Api.Shared;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class DeleteGameEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task DeleteGame_WhenAdministratorDeletesDoublesGame_ShouldDeleteGameAndAllContributions()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var firstMemberId = $"user_test_{Guid.NewGuid():N}";
        var secondMemberId = $"user_test_{Guid.NewGuid():N}";
        var thirdMemberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: firstMemberId)
            .WithMember(id: secondMemberId)
            .WithMember(id: thirdMemberId)
            .WithDoublesGame(
                side1: (administratorId, firstMemberId, 11),
                side2: (secondMemberId, thirdMemberId, 7)
            )
            .BuildAsync();
        var gameId = Assert.Single(context.Games).Uuid;
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.Games.AnyAsync(game => game.Uuid == gameId));
        Assert.False(await database.PlayerStatistics.AnyAsync(
            statistics => statistics.GameUuid == gameId
        ));
        Assert.False(await database.MatchupStatistics.AnyAsync(
            statistics => statistics.GameUuid == gameId
        ));
        Assert.False(await database.DoublesPairingStatistics.AnyAsync(
            statistics => statistics.GameUuid == gameId
        ));
        var winStreaks = await database.PlayerWinStreaks
            .Where(winStreak => winStreak.CrewUuid == context.Uuid)
            .ToArrayAsync();

        Assert.Equal(4, winStreaks.Length);
        Assert.All(winStreaks, winStreak =>
        {
            Assert.Equal(0, winStreak.CurrentWinStreak);
            Assert.Null(winStreak.CurrentWinStreakStartedAtUtc);
            Assert.Equal(0, winStreak.LongestWinStreak);
            Assert.Null(winStreak.LongestWinStreakStartedAtUtc);
            Assert.Null(winStreak.LongestWinStreakEndedAtUtc);
        });
    }

    [Fact]
    public async Task DeleteGame_WhenDeletedGameBreaksWinStreak_ShouldRecalculateStreaksAndInvalidateCaches()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGames(
                administratorId,
                memberId,
                (11, 7),
                (11, 8),
                (9, 11),
                (11, 6)
            )
            .BuildAsync();
        var deletedGame = context.Games[2];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var cachedHistory = await GetGamesAsync(apiClient);
        var cachedStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Equal(4, cachedHistory.TotalCount);
        Assert.Equal(1, cachedStatistics.CurrentWinStreak);
        Assert.Equal(2, cachedStatistics.LongestWinStreak);

        using var response = await apiClient.DeleteAsync($"/games/{deletedGame.Uuid}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var refreshedHistory = await GetGamesAsync(apiClient);
        var refreshedStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Equal(3, refreshedHistory.TotalCount);
        Assert.DoesNotContain(
            refreshedHistory.Items,
            game => game.GameId == deletedGame.Uuid
        );
        Assert.Equal(3, refreshedStatistics.CurrentWinStreak);
        Assert.Equal(3, refreshedStatistics.LongestWinStreak);
        Assert.Equal(context.Games[0].PlayedAtUtc, refreshedStatistics.CurrentWinStreakStartedAtUtc);
        Assert.Equal(context.Games[0].PlayedAtUtc, refreshedStatistics.LongestWinStreakStartedAtUtc);
        Assert.Null(refreshedStatistics.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task DeleteGame_WhenCallerIsNotAdministrator_ShouldReturnForbiddenAndKeepGame()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: (memberId, 7)
            )
            .BuildAsync();
        var gameId = Assert.Single(context.Games).Uuid;
        using var apiClient = fixture.CreateApiClient(context.Members[0]);

        using var response = await apiClient.DeleteAsync($"/games/{gameId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Games.AnyAsync(game => game.Uuid == gameId));
    }

    [Fact]
    public async Task DeleteGame_WhenGameBelongsToAnotherCrew_ShouldReturnNotFoundAndKeepGame()
    {
        var firstCrew = await fixture.CreateCrewBuilder().BuildAsync();
        var otherAdministratorId = $"user_test_{Guid.NewGuid():N}";
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherAdministratorId)
            .WithSinglesGame(
                side1: (otherAdministratorId, 11),
                side2: ("Guest player", 7)
            )
            .BuildAsync();
        var otherGameId = Assert.Single(otherCrew.Games).Uuid;
        using var apiClient = fixture.CreateApiClient(firstCrew.Administrator);

        using var response = await apiClient.DeleteAsync($"/games/{otherGameId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Games.AnyAsync(game => game.Uuid == otherGameId));
    }

    private static async Task<PaginatedHttpResponse<GameHistoryItemHttpResponse>> GetGamesAsync(
        HttpClient apiClient)
    {
        using var response = await apiClient.GetAsync("/games");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(TestJsonSerializerOptions.Default) ?? throw new InvalidOperationException(
            "The API returned an empty game history response."
        );
    }

    private static async Task<GetPersonalStatisticsHttpResponse> GetPersonalStatisticsAsync(
        HttpClient apiClient)
    {
        using var response = await apiClient.GetAsync("/statistics/personal");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException(
            "The API returned an empty personal statistics response."
        );
    }
}
