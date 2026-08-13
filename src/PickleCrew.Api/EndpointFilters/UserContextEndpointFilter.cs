using System.Security.Claims;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.EndpointFilters;

internal static class UserContextEndpointFilterExtensions
{
    public static RouteHandlerBuilder WithUserContext(this RouteHandlerBuilder builder)
    {
        return builder
            .RequireAuthorization(AuthorizationPolicies.User)
            .AddEndpointFilter<UserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    public static RouteGroupBuilder WithUserContext(this RouteGroupBuilder builder)
    {
        return builder
            .RequireAuthorization(AuthorizationPolicies.User)
            .AddEndpointFilter<UserContextEndpointFilter>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}

internal sealed class UserContextEndpointFilter(UserContext userContext) : IEndpointFilter
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

        userContext.UserId = userId;
        userContext.OrganizationId = context.HttpContext.User.FindFirstValue(
            WorkOsClaimTypes.OrganizationId
        );
        userContext.Role = context.HttpContext.User.FindFirstValue(WorkOsClaimTypes.Role);

        return await next(context);
    }
}
