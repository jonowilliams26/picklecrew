using System.Security.Claims;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.EndpointFilters;

internal static class WorkOsUserContextEndpointFilterExtensions
{
    public static RouteHandlerBuilder WithWorkOsUserContext(this RouteHandlerBuilder builder)
    {
        return builder
            .RequireAuthorization(AuthorizationPolicies.WorkOsUser)
            .AddEndpointFilter<WorkOsUserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}

internal sealed class WorkOsUserContextEndpointFilter(WorkOsUserContext workOsUserContext)
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.User.FindFirstValue(WorkOsClaimTypes.Subject);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return TypedResults.Unauthorized();
        }

        workOsUserContext.UserId = userId;

        return await next(context);
    }
}
