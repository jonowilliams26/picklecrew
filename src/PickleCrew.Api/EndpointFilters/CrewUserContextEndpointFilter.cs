using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using ZiggyCreatures.Caching.Fusion;

namespace PickleCrew.Api.EndpointFilters;

internal static class CrewUserContextEndpointFilterExtensions
{
    public static RouteHandlerBuilder WithCrewUserContext(this RouteHandlerBuilder builder)
    {
        return builder
            .RequireAuthorization(AuthorizationPolicies.CrewUser)
            .AddEndpointFilter<CrewUserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    public static RouteGroupBuilder WithCrewUserContext(this RouteGroupBuilder builder)
    {
        return builder
            .RequireAuthorization(AuthorizationPolicies.CrewUser)
            .AddEndpointFilter<CrewUserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

internal sealed class CrewUserContextEndpointFilter(
    UserContext userContext,
    CrewUserContext crewUserContext,
    PickleCrewDbContext database,
    IFusionCache cache)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.User.FindFirstValue(WorkOsClaimTypes.Subject);
        var organizationId = context.HttpContext.User.FindFirstValue(
            WorkOsClaimTypes.OrganizationId
        );
        var role = context.HttpContext.User.FindFirstValue(WorkOsClaimTypes.Role);
        var hasRequiredClaims = !string.IsNullOrWhiteSpace(userId)
            && !string.IsNullOrWhiteSpace(organizationId)
            && !string.IsNullOrWhiteSpace(role);

        if (!hasRequiredClaims)
        {
            return TypedResults.Unauthorized();
        }

        var requiredUserId = userId!;
        var requiredOrganizationId = organizationId!;
        var requiredRole = role!;
        var cacheOption = new CrewUserContextCacheOption(requiredOrganizationId);
        var cachedCrew = await cache.TryGetAsync<CrewUserContextCacheEntry>(
            cacheOption.Key,
            token: context.HttpContext.RequestAborted
        );
        CrewUserContextCacheEntry crew;

        if (cachedCrew.HasValue)
        {
            crew = cachedCrew.Value;
        }
        else
        {
            var databaseCrew = await database.Crews
                .IgnoreQueryFilters()
                .Where(candidate =>
                    candidate.WorkOsOrganizationId == requiredOrganizationId
                )
                .Select(candidate => new CrewUserContextCacheEntry
                {
                    CrewId = candidate.Id,
                    CrewUuid = candidate.Uuid,
                })
                .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

            if (databaseCrew is null)
            {
                return TypedResults.Forbid();
            }

            crew = databaseCrew;
            await cache.SetAsync(
                cacheOption.Key,
                crew,
                token: context.HttpContext.RequestAborted
            );
        }

        userContext.UserId = requiredUserId;
        userContext.OrganizationId = requiredOrganizationId;
        userContext.Role = requiredRole;

        crewUserContext.UserId = requiredUserId;
        crewUserContext.OrganizationId = requiredOrganizationId;
        crewUserContext.Role = requiredRole;
        crewUserContext.CrewId = crew.CrewId;
        crewUserContext.CrewUuid = crew.CrewUuid;

        return await next(context);
    }
}

internal sealed record CrewUserContextCacheEntry
{
    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }
}
