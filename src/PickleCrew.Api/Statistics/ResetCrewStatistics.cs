using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record ResetCrewStatisticsHttpRequest
{
    public required string Confirmation { get; init; }
}

internal sealed class ResetCrewStatisticsEndpoint : IEndpoint
{
    internal const string ConfirmationPhrase = "reset statistics";

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/reset", HandleAsync)
            .WithName("ResetCrewStatistics")
            .WithSummary("Deletes every Game and all statistics for the active Crew.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator);
    }

    private static async Task<Results<NoContent, ValidationProblem>> HandleAsync(
        ResetCrewStatisticsHttpRequest request,
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var hasValidConfirmation = string.Equals(
            request.Confirmation,
            ConfirmationPhrase,
            StringComparison.Ordinal
        );

        if (!hasValidConfirmation)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Confirmation)] = [
                    $"Enter {ConfirmationPhrase} exactly to confirm.",
                ],
            });
        }

        var executionStrategy = database.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                await database.Database.ExecuteSqlRawAsync(
                    """
                    LOCK TABLE
                        "Games",
                        "PlayerStatistics",
                        "DoublesPairingStatistics",
                        "MatchupStatistics",
                        "PlayerWinStreaks"
                    IN ACCESS EXCLUSIVE MODE;
                    """,
                    transactionCancellationToken
                );

                await database.MatchupStatistics.ExecuteDeleteAsync(
                    transactionCancellationToken
                );
                await database.DoublesPairingStatistics.ExecuteDeleteAsync(
                    transactionCancellationToken
                );
                await database.PlayerStatistics.ExecuteDeleteAsync(
                    transactionCancellationToken
                );
                await database.PlayerWinStreaks.ExecuteDeleteAsync(
                    transactionCancellationToken
                );
                await database.Games.ExecuteDeleteAsync(
                    transactionCancellationToken
                );

                return true;
            },
            static _ => Task.FromResult(false),
            cancellationToken
        );

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
