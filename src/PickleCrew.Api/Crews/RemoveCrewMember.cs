using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Crews;

public sealed record RemoveCrewMemberHttpRequest
{
    public required string UserId { get; init; }
}

internal sealed class RemoveCrewMemberEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/members/{userId}", HandleAsync)
            .WithName("RemoveCrewMember")
            .WithSummary("Removes an active member from the active crew.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator);
    }

    private static async Task<Results<
        NoContent,
        NotFound,
        ForbidHttpResult>> HandleAsync(
        [AsParameters] RemoveCrewMemberHttpRequest request,
        CrewUserContext crewUserContext,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        if (string.Equals(
            request.UserId,
            crewUserContext.UserId,
            StringComparison.Ordinal
        ))
        {
            return TypedResults.Forbid();
        }

        var wasRemoved = await crewService.RemoveCrewMemberAsync(
            request.UserId,
            cancellationToken
        );

        if (!wasRemoved)
        {
            return TypedResults.NotFound();
        }

        await cache.RemoveByTagAsync(
            new CrewStatisticsCacheTag(crewUserContext.CrewUuid),
            cancellationToken
        );

        return TypedResults.NoContent();
    }
}
