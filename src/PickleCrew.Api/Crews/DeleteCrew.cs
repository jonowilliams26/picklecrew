using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record DeleteCrewHttpRequest
{
    public required string CrewName { get; init; }
}

internal sealed class DeleteCrewEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete(string.Empty, HandleAsync)
            .WithName("DeleteCrew")
            .WithSummary("Permanently deletes the active crew after confirming its name.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator);
    }

    private static async Task<Results<NoContent, ValidationProblem>> HandleAsync(
        [FromBody] DeleteCrewHttpRequest request,
        CrewUserContext crewUserContext,
        PickleCrewDbContext database,
        IWorkOsService workOs,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var crewName = await database.Crews
            .Where(crew => crew.Id == crewUserContext.CrewId)
            .Select(crew => crew.Name)
            .SingleAsync(cancellationToken);

        if (!string.Equals(request.CrewName, crewName, StringComparison.Ordinal))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.CrewName)] = ["Enter the crew name exactly to confirm."],
            });
        }

        await workOs.DeleteOrganizationAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );

        var executionStrategy = database.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteInTransactionAsync(
            async operationCancellationToken =>
            {
                await database.Crews
                    .Where(crew => crew.Id == crewUserContext.CrewId)
                    .ExecuteDeleteAsync(operationCancellationToken);
                await database.CrewProvisioningAttempts
                    .Where(attempt => attempt.CrewUuid == crewUserContext.CrewUuid)
                    .ExecuteDeleteAsync(operationCancellationToken);
            },
            static _ => Task.FromResult(false),
            cancellationToken
        );

        await cache.RemoveAsync(
            new CrewUserContextCacheOption(crewUserContext.OrganizationId).Key,
            cancellationToken
        );
        await cache.RemoveByTagAsync(
            new CrewCacheTag(crewUserContext.CrewUuid),
            cancellationToken
        );

        return TypedResults.NoContent();
    }
}
