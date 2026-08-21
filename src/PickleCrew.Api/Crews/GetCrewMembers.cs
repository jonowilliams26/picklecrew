using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Shared;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record GetCrewMembersHttpRequest : PaginationOptions;

public sealed record GetCrewMembersHttpResponse
{
    public required CrewMemberHttpResponse[] Members { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}

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

    private static async Task<Ok<GetCrewMembersHttpResponse>> HandleAsync(
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

        return TypedResults.Ok(new GetCrewMembersHttpResponse
        {
            Members = paginatedMembers.Items,
            Page = paginatedMembers.Page,
            PageSize = paginatedMembers.PageSize,
            TotalCount = paginatedMembers.TotalCount,
            TotalPages = paginatedMembers.TotalPages,
        });
    }
}
