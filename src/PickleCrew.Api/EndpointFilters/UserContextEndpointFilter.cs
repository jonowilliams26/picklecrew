using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.EndpointFilters;

internal static class UserContextEndpointFilterExtensions
{
    public static RouteGroupBuilder WithUserContext(this RouteGroupBuilder builder)
    {
        return builder
            .WithWorkOsUserContext()
            .AddEndpointFilter<UserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

internal sealed class UserContextEndpointFilter(
    WorkOsUserContext workOsUserContext,
    UserContext userContext,
    PickleCrewDbContext dbContext)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var organizationId = workOsUserContext.OrganizationId;
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return TypedResults.Unauthorized();
        }

        var role = workOsUserContext.Role;
        if (string.IsNullOrWhiteSpace(role))
        {
            return TypedResults.Unauthorized();
        }

        var crew = await dbContext.Crews
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

        userContext.UserId = workOsUserContext.UserId;
        userContext.CrewId = crew.Id;
        userContext.CrewUuid = crew.Uuid;
        userContext.Role = role;

        return await next(context);
    }
}
