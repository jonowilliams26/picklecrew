using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Games;

public sealed record DeleteGameHttpRequest
{
    public required Guid GameId { get; init; }
}

internal sealed class DeleteGameEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/{gameId:guid}", HandleAsync)
            .WithName("DeleteGame")
            .WithSummary("Deletes a completed Game from the active Crew.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator);
    }

    private static async Task<Results<NoContent, NotFound>> HandleAsync(
        [AsParameters] DeleteGameHttpRequest request,
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var game = await database.Games
            .SingleOrDefaultAsync(
                candidate => candidate.Uuid == request.GameId,
                cancellationToken
            );

        if (game is null)
        {
            return TypedResults.NotFound();
        }

        database.Games.Remove(game);

        var remainingGames = await database.Games
            .AsNoTracking()
            .Where(candidate => candidate.Uuid != request.GameId)
            .OrderBy(candidate => candidate.PlayedAtUtc)
            .ThenBy(candidate => candidate.Id)
            .ToArrayAsync(cancellationToken);

        var winStreaks = await database.PlayerWinStreaks
            .ToArrayAsync(cancellationToken);

        // Reset all win streaks
        foreach (var winStreak in winStreaks)
        {
            winStreak.CurrentWinStreak = 0;
            winStreak.CurrentWinStreakStartedAtUtc = null;
            winStreak.LongestWinStreak = 0;
            winStreak.LongestWinStreakStartedAtUtc = null;
            winStreak.LongestWinStreakEndedAtUtc = null;
        }

        // Reapply all remaining games to the win streaks
        foreach (var remainingGame in remainingGames)
        {
            foreach (var winStreak in winStreaks)
            {
                winStreak.ApplyGameResult(remainingGame);
            }
        }

        await database.SaveChangesAsync(cancellationToken);

        await cache.RemoveByTagAsync(
            [
                new CrewGamesCacheTag(crewUserContext.CrewUuid),
                new CrewStatisticsCacheTag(crewUserContext.CrewUuid),
            ],
            cancellationToken
        );

        return TypedResults.NoContent();
    }
}
