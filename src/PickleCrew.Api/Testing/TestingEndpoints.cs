using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Extensions;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Testing;

internal static class TestingEndpoints
{
    public static IEndpointRouteBuilder MapTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsTesting())
        {
            return app;
        }

        app.MapGet("/testing/work-os-user-context", GetWorkOsUserContext)
            .WithWorkOsUserContext();

        return app;
    }

    private static Ok<TestWorkOsUserContextHttpResponse> GetWorkOsUserContext(
        WorkOsUserContext context)
    {
        return TypedResults.Ok(
            new TestWorkOsUserContextHttpResponse
            {
                UserId = context.UserId,
                OrganizationId = context.OrganizationId,
                Role = context.Role,
            }
        );
    }
}

internal sealed record TestWorkOsUserContextHttpResponse
{
    public required string UserId { get; init; }

    public string? OrganizationId { get; init; }

    public string? Role { get; init; }
}
