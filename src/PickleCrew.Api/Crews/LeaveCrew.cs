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

public sealed record GetCrewLeavingEligibilityHttpResponse
{
    public required bool CanLeave { get; init; }
}

internal sealed class LeaveCrewEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/leave-eligibility", GetEligibilityAsync)
            .WithName("GetCrewLeavingEligibility")
            .WithSummary("Gets whether the current member may leave the active crew.");

        app.MapPost("/leave", LeaveAsync)
            .WithName("LeaveCrew")
            .WithSummary("Leaves the active crew after confirming its name.");
    }

    private static async Task<Results<
        Ok<GetCrewLeavingEligibilityHttpResponse>,
        NotFound>> GetEligibilityAsync(
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var eligibility = await crewService.GetCrewLeavingEligibilityAsync(
            cancellationToken
        );

        if (eligibility == CrewLeavingEligibility.MembershipNotFound)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new GetCrewLeavingEligibilityHttpResponse
        {
            CanLeave = eligibility == CrewLeavingEligibility.Eligible,
        });
    }

    private static async Task<Results<
        NoContent,
        ValidationProblem,
        NotFound,
        Conflict>> LeaveAsync(
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

        var outcome = await crewService.LeaveCrewAsync(cancellationToken);

        if (outcome == LeaveCrewOutcome.MembershipNotFound)
        {
            return TypedResults.NotFound();
        }

        if (outcome == LeaveCrewOutcome.SoleAdministrator)
        {
            return TypedResults.Conflict();
        }

        await cache.RemoveByTagAsync(
            new CrewStatisticsCacheTag(crewUserContext.CrewUuid),
            cancellationToken
        );

        return TypedResults.NoContent();
    }
}
