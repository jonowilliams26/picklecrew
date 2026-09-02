using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Crews;

public sealed record ChangeCrewMemberRoleHttpRequest
{
    public required string UserId { get; init; }

    [FromBody]
    public required ChangeCrewMemberRoleHttpRequestBody Body { get; init; }
}

public sealed record ChangeCrewMemberRoleHttpRequestBody
{
    public required CrewMemberRole Role { get; init; }
}

public enum CrewMemberRole
{
    Admin,
    Member,
}

internal sealed class ChangeCrewMemberRoleEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/members/{userId}/role", HandleAsync)
            .WithName("ChangeCrewMemberRole")
            .WithSummary("Changes another active crew member's role.")
            .RequireAuthorization(AuthorizationPolicies.CrewAdministrator)
            .WithRequestValidation<ChangeCrewMemberRoleHttpRequest>(Validate);
    }

    internal static Dictionary<string, string[]> Validate(
        ChangeCrewMemberRoleHttpRequest request)
    {
        if (!Enum.IsDefined(request.Body.Role))
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.Body.Role)] = ["Select a valid crew member role."],
            };
        }

        return [];
    }

    private static async Task<Results<
        NoContent,
        NotFound,
        ForbidHttpResult>> HandleAsync(
        [AsParameters] ChangeCrewMemberRoleHttpRequest request,
        CrewUserContext crewUserContext,
        ICrewService crewService,
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

        var roleSlug = request.Body.Role switch
        {
            CrewMemberRole.Admin => WorkOsRoles.Admin,
            CrewMemberRole.Member => WorkOsRoles.Member,
            _ => throw new InvalidOperationException(
                $"Unsupported crew member role '{request.Body.Role}'."
            ),
        };
        var wasChanged = await crewService.ChangeCrewMemberRoleAsync(
            request.UserId,
            roleSlug,
            cancellationToken
        );

        return wasChanged
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
