using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Games;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetRecentGamesHttpResponse
{
    public required RecentGameItemHttpResponse[] Games { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int WinStreak { get; init; }
}

public sealed record RecentGameItemHttpResponse
{
    public required Guid GameId { get; init; }

    public required GameFormat Format { get; init; }

    public required DateTimeOffset PlayedAtUtc { get; init; }

    public required RecentGameResult Result { get; init; }
}

public enum RecentGameResult
{
    Win,
    Loss,
}

internal sealed class GetRecentGamesEndpoint : IEndpoint
{
    private const int RecentGameLimit = 10;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/recent-games", HandleAsync)
            .WithName("GetRecentGames")
            .WithSummary("Gets the signed-in user's ten most recent Games for the active Crew.");
    }

    private static async Task<Ok<GetRecentGamesHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new PersonalStatisticsCacheOption(
                crewUserContext.CrewUuid,
                crewUserContext.UserId,
                PersonalStatisticsCacheResource.RecentGames
            ),
            crewUserContext,
            scopeFactory,
            static (services, factoryCancellationToken) => GetResponseAsync(
                services.GetRequiredService<CrewUserContext>().UserId,
                services.GetRequiredService<CrewScopedDbContext>(),
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetRecentGamesHttpResponse> GetResponseAsync(
        string userId,
        CrewScopedDbContext database,
        CancellationToken cancellationToken)
    {
        var games = await database.Games
            .AsNoTracking()
            .Where(game => game.Sides.Any(side => side.Participants.Any(participant =>
                participant.UserId == userId
            )))
            .OrderByDescending(game => game.PlayedAtUtc)
            .ThenByDescending(game => game.Id)
            .Take(RecentGameLimit)
            .ToArrayAsync(cancellationToken);

        var recentGames = games
            .Select(game => ToHttpResponse(game, userId))
            .ToArray();

        var results = recentGames
            .Select(game => game.Result)
            .ToArray();

        var summary = RecentGamesSummaryCalculator.Calculate(results);

        return new GetRecentGamesHttpResponse
        {
            Games = recentGames,
            Wins = summary.Wins,
            Losses = summary.Losses,
            WinStreak = summary.WinStreak,
        };
    }

    private static RecentGameItemHttpResponse ToHttpResponse(
        Game game,
        string userId)
    {
        var userSide = game.Sides.Single(side => side.Participants.Any(participant =>
            string.Equals(participant.UserId, userId, StringComparison.Ordinal)
        ));
        var opposingSide = game.Sides.Single(side =>
            side.SideNumber != userSide.SideNumber
        );
        var result = userSide.Score > opposingSide.Score
            ? RecentGameResult.Win
            : RecentGameResult.Loss;

        return new RecentGameItemHttpResponse
        {
            GameId = game.Uuid,
            Format = game.Format,
            PlayedAtUtc = game.PlayedAtUtc,
            Result = result,
        };
    }
}

internal static class RecentGamesSummaryCalculator
{
    public static RecentGamesSummary Calculate(RecentGameResult[] results)
    {
        var wins = results.Count(result => result == RecentGameResult.Win);
        var losses = results.Length - wins;
        var winStreak = results
            .TakeWhile(result => result == RecentGameResult.Win)
            .Count();

        return new RecentGamesSummary
        {
            Wins = wins,
            Losses = losses,
            WinStreak = winStreak,
        };
    }
}

internal sealed record RecentGamesSummary
{
    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int WinStreak { get; init; }
}
