using System.Net.Http.Json;
using PickleCrew.Api.Data;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetRecentGamesEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetRecentGames_WhenMoreThanTenGamesExist_ShouldReturnBoundedParticipatingCrewSummary()
    {
        var crew = await fixture.CreateCrewBuilder()
            .WithMember(displayName: "Current Opponent")
            .BuildAsync();
        var otherCrew = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var opponent = Assert.Single(crew.Members);
        var newestPlayedAt = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var results = new[]
        {
            true, true, false, true, false, true, true, false, true, true, true, true,
        };
        var games = results
            .Select((won, index) => StatisticsTestData.CreateSinglesGame(
                crew,
                crew.Administrator.UserId,
                opponent.UserId,
                won ? 11 : 8,
                won ? 7 : 11,
                newestPlayedAt.AddMinutes(-index),
                crew.Administrator.UserId
            ))
            .ToArray();
        var recorderOnlyGame = StatisticsTestData.CreateSinglesGame(
            crew,
            opponent.UserId,
            null,
            11,
            5,
            newestPlayedAt.AddMinutes(1),
            crew.Administrator.UserId
        );
        var otherCrewGame = StatisticsTestData.CreateSinglesGame(
            otherCrew,
            crew.Administrator.UserId,
            null,
            11,
            5,
            newestPlayedAt.AddMinutes(2),
            otherCrew.Administrator.UserId
        );

        await using (var database = fixture.CreateDbContext())
        {
            database.Games.AddRange(games);
            database.Games.AddRange(recorderOnlyGame, otherCrewGame);
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(crew.Administrator);
        using var response = await apiClient.GetAsync("/statistics/recent-games");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<GetRecentGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(summary);
        Assert.Equal(10, summary.Games.Length);
        Assert.Equal(7, summary.Wins);
        Assert.Equal(3, summary.Losses);
        Assert.Equal(2, summary.WinStreak);
        Assert.Equal(games[0].Uuid, summary.Games[0].GameId);
        Assert.Equal(games[9].Uuid, summary.Games[9].GameId);
        Assert.Equal(newestPlayedAt, summary.Games[0].PlayedAtUtc);
        Assert.Equal(GameFormat.Singles, summary.Games[0].Format);
        Assert.Equal(RecentGameResult.Win, summary.Games[0].Result);
        Assert.DoesNotContain(summary.Games, game => game.GameId == recorderOnlyGame.Uuid);
        Assert.DoesNotContain(summary.Games, game => game.GameId == otherCrewGame.Uuid);
        Assert.DoesNotContain(summary.Games, game => game.GameId == games[10].Uuid);
    }

    [Fact]
    public async Task GetRecentGames_WhenPlayedTimesMatch_ShouldOrderByDatabaseIdDescending()
    {
        var crew = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var playedAt = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
        var firstGame = StatisticsTestData.CreateSinglesGame(
            crew,
            crew.Administrator.UserId,
            null,
            11,
            5,
            playedAt,
            crew.Administrator.UserId
        );
        var secondGame = StatisticsTestData.CreateSinglesGame(
            crew,
            crew.Administrator.UserId,
            null,
            7,
            11,
            playedAt,
            crew.Administrator.UserId
        );

        await using (var database = fixture.CreateDbContext())
        {
            database.Games.AddRange(firstGame, secondGame);
            await database.SaveChangesAsync();
        }

        using var apiClient = fixture.CreateApiClient(crew.Administrator);
        using var response = await apiClient.GetAsync("/statistics/recent-games");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<GetRecentGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(summary);
        Assert.Collection(
            summary.Games,
            game => Assert.Equal(secondGame.Uuid, game.GameId),
            game => Assert.Equal(firstGame.Uuid, game.GameId)
        );
        Assert.Equal(1, summary.Wins);
        Assert.Equal(1, summary.Losses);
        Assert.Equal(0, summary.WinStreak);
    }

    [Fact]
    public async Task GetRecentGames_WhenNoParticipatingGamesExist_ShouldReturnEmptySummary()
    {
        var crew = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(crew.Administrator);

        using var response = await apiClient.GetAsync("/statistics/recent-games");

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<GetRecentGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(summary);
        Assert.Empty(summary.Games);
        Assert.Equal(0, summary.Wins);
        Assert.Equal(0, summary.Losses);
        Assert.Equal(0, summary.WinStreak);
    }
}
