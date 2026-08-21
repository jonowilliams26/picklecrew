using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Shared;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record GetCrewMembersHttpRequest : PaginatedHttpRequest;

public sealed record CrewMemberHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required string Role { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed class GetCrewMembersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/members", HandleAsync)
            .WithName("GetCrewMembers")
            .WithSummary("Gets a paginated roster of active crew members.")
            .WithRequestValidation<GetCrewMembersHttpRequest>(Pagination.Validate);
    }

    private static async Task<Ok<PaginatedHttpResponse<CrewMemberHttpResponse>>> HandleAsync(
        [AsParameters] GetCrewMembersHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var activeMembers = await workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );
        var paginatedMembers = activeMembers
            .OrderBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.EmailAddress, StringComparer.OrdinalIgnoreCase)
            .Select(member => new CrewMemberHttpResponse
            {
                UserId = member.WorkOsUserId,
                DisplayName = member.DisplayName,
                EmailAddress = member.EmailAddress,
                Role = member.RoleSlug,
                ProfilePictureUrl = member.ProfilePictureUrl,
            })
            .Paginate(request);

        return TypedResults.Ok(paginatedMembers);
    }
}
