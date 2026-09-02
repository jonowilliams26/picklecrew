using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Games;
using PickleCrew.Api.Shared;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class ResetCrewStatisticsEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task ResetCrewStatistics_WhenAdministratorConfirms_ShouldDeleteActiveCrewGamesAndStatistics()
    {
        var firstAdministratorId = $"user_test_{Guid.NewGuid():N}";
        var firstPlayer2Id = $"user_test_{Guid.NewGuid():N}";
        var firstPlayer3Id = $"user_test_{Guid.NewGuid():N}";
        var firstPlayer4Id = $"user_test_{Guid.NewGuid():N}";
        var firstCrew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: firstAdministratorId)
            .WithMember(id: firstPlayer2Id)
            .WithMember(id: firstPlayer3Id)
            .WithMember(id: firstPlayer4Id)
            .WithDoublesGame(
                side1: (firstAdministratorId, firstPlayer2Id, 11),
                side2: (firstPlayer3Id, firstPlayer4Id, 7)
            )
            .BuildAsync();
        var secondAdministratorId = $"user_test_{Guid.NewGuid():N}";
        var secondPlayer2Id = $"user_test_{Guid.NewGuid():N}";
        var secondPlayer3Id = $"user_test_{Guid.NewGuid():N}";
        var secondPlayer4Id = $"user_test_{Guid.NewGuid():N}";
        var secondCrew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: secondAdministratorId)
            .WithMember(id: secondPlayer2Id)
            .WithMember(id: secondPlayer3Id)
            .WithMember(id: secondPlayer4Id)
            .WithDoublesGame(
                side1: (secondAdministratorId, secondPlayer2Id, 11),
                side2: (secondPlayer3Id, secondPlayer4Id, 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(firstCrew.Administrator);

        var initialGames = await GetGamesAsync(apiClient);
        var initialStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Equal(1, initialGames.TotalCount);
        Assert.Equal(1, initialStatistics.GamesPlayed);

        using var response = await apiClient.PostAsJsonAsync(
            "/statistics/reset",
            new ResetCrewStatisticsHttpRequest
            {
                Confirmation = "reset statistics",
            }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var refreshedGames = await GetGamesAsync(apiClient);
        var refreshedStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Equal(0, refreshedGames.TotalCount);
        Assert.Equal(0, refreshedStatistics.GamesPlayed);

        await using var database = fixture.CreateDbContext();
        Assert.False(await database.Games.AnyAsync(game =>
            game.CrewId == firstCrew.Id
        ));
        Assert.False(await database.PlayerStatistics.AnyAsync(statistics =>
            statistics.CrewId == firstCrew.Id
        ));
        Assert.False(await database.DoublesPairingStatistics.AnyAsync(statistics =>
            statistics.CrewId == firstCrew.Id
        ));
        Assert.False(await database.MatchupStatistics.AnyAsync(statistics =>
            statistics.CrewId == firstCrew.Id
        ));
        Assert.False(await database.PlayerWinStreaks.AnyAsync(winStreak =>
            winStreak.CrewId == firstCrew.Id
        ));

        Assert.True(await database.Games.AnyAsync(game =>
            game.CrewId == secondCrew.Id
        ));
        Assert.True(await database.PlayerStatistics.AnyAsync(statistics =>
            statistics.CrewId == secondCrew.Id
        ));
        Assert.True(await database.DoublesPairingStatistics.AnyAsync(statistics =>
            statistics.CrewId == secondCrew.Id
        ));
        Assert.True(await database.MatchupStatistics.AnyAsync(statistics =>
            statistics.CrewId == secondCrew.Id
        ));
        Assert.True(await database.PlayerWinStreaks.AnyAsync(winStreak =>
            winStreak.CrewId == secondCrew.Id
        ));
    }

    [Fact]
    public async Task ResetCrewStatistics_WhenConfirmationDoesNotMatchExactly_ShouldReturnBadRequestAndPreserveData()
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
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/statistics/reset",
            new ResetCrewStatisticsHttpRequest
            {
                Confirmation = "Reset statistics",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Games.AnyAsync(game =>
            game.CrewId == context.Id
        ));
        Assert.True(await database.PlayerStatistics.AnyAsync(statistics =>
            statistics.CrewId == context.Id
        ));
        Assert.True(await database.PlayerWinStreaks.AnyAsync(winStreak =>
            winStreak.CrewId == context.Id
        ));
    }

    [Fact]
    public async Task ResetCrewStatistics_WhenMemberRequestsReset_ShouldReturnForbiddenAndPreserveData()
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
        var member = Assert.Single(context.Members);
        using var apiClient = fixture.CreateApiClient(member);

        using var response = await apiClient.PostAsJsonAsync(
            "/statistics/reset",
            new ResetCrewStatisticsHttpRequest
            {
                Confirmation = "reset statistics",
            }
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        await using var database = fixture.CreateDbContext();
        Assert.True(await database.Games.AnyAsync(game =>
            game.CrewId == context.Id
        ));
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
