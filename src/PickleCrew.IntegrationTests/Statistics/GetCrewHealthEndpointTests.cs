using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class GetCrewHealthEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetCrewHealth_WhenCrewHasGamesAcrossDateRange_ShouldReturnDailyCountsForLast365Days()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-364);
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var crew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGames(
                side1: administratorId,
                side2: "Health Guest",
                (11, 3),
                (11, 4),
                (11, 5),
                (11, 6),
                (11, 7),
                (11, 8)
            )
            .BuildAsync();
        var otherAdministratorId = $"user_test_{Guid.NewGuid():N}";
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherAdministratorId)
            .WithSinglesGame(
                side1: (otherAdministratorId, 11),
                side2: ("Other Crew Guest", 6)
            )
            .BuildAsync();

        await using (var database = fixture.CreateDbContext())
        {
            var playedDates = new[]
            {
                today,
                today,
                today.AddDays(-1),
                startDate,
                startDate.AddDays(-1),
                today.AddDays(1),
            };

            for (var index = 0; index < crew.Games.Length; index++)
            {
                var gameId = crew.Games[index].Uuid;
                var playedAtUtc = new DateTimeOffset(
                    playedDates[index].ToDateTime(new TimeOnly(12, 0)),
                    TimeSpan.Zero
                );

                await database.Games
                    .Where(game => game.Uuid == gameId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(game => game.PlayedAtUtc, playedAtUtc)
                    );
            }

            var otherCrewGameId = Assert.Single(otherCrew.Games).Uuid;
            var otherCrewPlayedAtUtc = new DateTimeOffset(
                today.ToDateTime(new TimeOnly(12, 0)),
                TimeSpan.Zero
            );
            await database.Games
                .Where(game => game.Uuid == otherCrewGameId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(game => game.PlayedAtUtc, otherCrewPlayedAtUtc)
                );
        }

        using var apiClient = fixture.CreateApiClient(crew.Administrator);
        using var response = await apiClient.GetAsync("/statistics/crew-health");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewHealthHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(startDate, result.StartDate);
        Assert.Equal(today, result.EndDate);
        Assert.Collection(
            result.Days,
            day =>
            {
                Assert.Equal(startDate, day.Date);
                Assert.Equal(1, day.GameCount);
            },
            day =>
            {
                Assert.Equal(today.AddDays(-1), day.Date);
                Assert.Equal(1, day.GameCount);
            },
            day =>
            {
                Assert.Equal(today, day.Date);
                Assert.Equal(2, day.GameCount);
            }
        );
    }

    [Fact]
    public async Task GetCrewHealth_WhenCrewHasNoGames_ShouldReturnEmptyDailyCounts()
    {
        var crew = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(crew.Administrator);

        using var response = await apiClient.GetAsync("/statistics/crew-health");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetCrewHealthHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(364, result.EndDate.DayNumber - result.StartDate.DayNumber);
        Assert.Empty(result.Days);
    }
}
