using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

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
    PickleCrewDbContext database)
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

        var crew = await database.Crews
            .IgnoreQueryFilters()
            .Where(candidate => candidate.WorkOsOrganizationId == organizationId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.Uuid,
            })
            .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

        if (crew is null)
        {
            return TypedResults.Forbid();
        }

        var requiredUserId = userId!;
        var requiredOrganizationId = organizationId!;
        var requiredRole = role!;

        userContext.UserId = requiredUserId;
        userContext.OrganizationId = requiredOrganizationId;
        userContext.Role = requiredRole;

        crewUserContext.UserId = requiredUserId;
        crewUserContext.OrganizationId = requiredOrganizationId;
        crewUserContext.Role = requiredRole;
        crewUserContext.CrewId = crew.Id;
        crewUserContext.CrewUuid = crew.Uuid;

        return await next(context);
    }
}
