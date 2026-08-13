using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Extensions;
using PickleCrew.Api.Services;

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

        app.MapGet("/testing/user-context", GetUserContext)
            .WithUserContext();
        app.MapGet("/testing/crew-user-context", GetCrewUserContext)
            .WithCrewUserContext();

        return app;
    }

    private static Ok<TestUserContextHttpResponse> GetUserContext(UserContext context)
    {
        return TypedResults.Ok(
            new TestUserContextHttpResponse
            {
                UserId = context.UserId,
                OrganizationId = context.OrganizationId,
                Role = context.Role,
            }
        );
    }

    private static Ok<TestCrewUserContextHttpResponse> GetCrewUserContext(
        CrewUserContext context)
    {
        return TypedResults.Ok(
            new TestCrewUserContextHttpResponse
            {
                UserId = context.UserId,
                OrganizationId = context.OrganizationId,
                Role = context.Role,
                CrewId = context.CrewId,
                CrewUuid = context.CrewUuid,
            }
        );
    }
}

internal sealed record TestUserContextHttpResponse
{
    public required string UserId { get; init; }

    public string? OrganizationId { get; init; }

    public string? Role { get; init; }
}

internal sealed record TestCrewUserContextHttpResponse
{
    public required string UserId { get; init; }

    public required string OrganizationId { get; init; }

    public required string Role { get; init; }

    public required long CrewId { get; init; }

    public required Guid CrewUuid { get; init; }
}
