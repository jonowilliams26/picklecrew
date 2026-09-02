using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Crews;

public sealed record LeaveCrewHttpRequest
{
    public required string CrewName { get; init; }
}

internal sealed class LeaveCrewEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/leave", HandleAsync)
            .WithName("LeaveCrew")
            .WithSummary("Leaves the active crew after confirming its name.");
    }

    private static async Task<Results<
        NoContent,
        ValidationProblem,
        NotFound>> HandleAsync(
        LeaveCrewHttpRequest request,
        CrewUserContext crewUserContext,
        PickleCrewDbContext database,
        ICrewService crewService,
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

        var wasMembershipDeactivated = await crewService.LeaveCrewAsync(
            cancellationToken
        );

        if (!wasMembershipDeactivated)
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
