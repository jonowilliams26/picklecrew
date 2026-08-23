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
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var opponentId = $"user_test_{Guid.NewGuid():N}";
        var results = new[]
        {
            true, true, false, true, false, true, true, false, true, true, true, true,
        };
        var crewBuilder = fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: opponentId, displayName: "Current Opponent");

        foreach (var won in results.Reverse())
        {
            crewBuilder.WithSinglesGame(
                side1: (administratorId, won ? 11 : 8),
                side2: (opponentId, won ? 7 : 11)
            );
        }

        crewBuilder.WithSinglesGame(
            side1: (opponentId, 11),
            side2: ("Recorder Only Guest", 5)
        );
        var crew = await crewBuilder.BuildAsync();
        var games = crew.Games
            .Take(results.Length)
            .Reverse()
            .ToArray();
        var recorderOnlyGame = crew.Games[^1];
        var otherAdministratorId = $"user_test_{Guid.NewGuid():N}";
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherAdministratorId)
            .WithMember(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Other Crew Guest", 5)
            )
            .BuildAsync();
        var otherCrewGame = Assert.Single(otherCrew.Games);

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
        Assert.Equal(games[0].PlayedAtUtc, summary.Games[0].PlayedAtUtc);
        Assert.Equal(GameFormat.Singles, summary.Games[0].Format);
        Assert.Equal(RecentGameResult.Win, summary.Games[0].Result);
        Assert.DoesNotContain(
            summary.Games,
            game => game.GameId == recorderOnlyGame.Uuid
        );
        Assert.DoesNotContain(
            summary.Games,
            game => game.GameId == otherCrewGame.Uuid
        );
        Assert.DoesNotContain(summary.Games, game => game.GameId == games[10].Uuid);
    }

    [Fact]
    public async Task GetRecentGames_WhenGamesAreRecordedSequentially_ShouldReturnNewestFirst()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var crew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("First Guest", 5)
            )
            .WithSinglesGame(
                side1: (administratorId, 7),
                side2: ("Second Guest", 11)
            )
            .BuildAsync();
        var firstGame = crew.Games[0];
        var secondGame = crew.Games[1];

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
