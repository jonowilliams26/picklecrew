using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.EndpointFilters;
using PickleCrew.Api.Services;
using PickleCrew.Api.Shared;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Crews;

public sealed record GetCrewMembersHttpRequest : PaginatedHttpRequest;

public sealed record CrewMemberHttpResponse
{
    public required string Id { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required CrewRosterEntryStatus Status { get; init; }

    public string? Role { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

public enum CrewRosterEntryStatus
{
    Active,
    Pending,
}

internal sealed class GetCrewMembersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/members", HandleAsync)
            .WithName("GetCrewMembers")
            .WithSummary("Gets a paginated roster of active members and pending invitations.")
            .WithRequestValidation<GetCrewMembersHttpRequest>(Pagination.Validate);
    }

    private static async Task<Ok<PaginatedHttpResponse<CrewMemberHttpResponse>>> HandleAsync(
        [AsParameters] GetCrewMembersHttpRequest request,
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var roster = await workOs.ListCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );
        var activeRosterEntries = roster.ActiveMembers.Select(member => new CrewMemberHttpResponse
        {
            Id = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            EmailAddress = member.EmailAddress,
            Status = CrewRosterEntryStatus.Active,
            Role = member.RoleSlug,
            ProfilePictureUrl = member.ProfilePictureUrl,
        });
        var pendingRosterEntries = roster.PendingInvitations.Select(
            invitation => new CrewMemberHttpResponse
            {
                Id = invitation.WorkOsInvitationId,
                DisplayName = invitation.EmailAddress,
                EmailAddress = invitation.EmailAddress,
                Status = CrewRosterEntryStatus.Pending,
            }
        );
        var paginatedRoster = activeRosterEntries
            .Concat(pendingRosterEntries)
            .OrderBy(entry => entry.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.EmailAddress, StringComparer.OrdinalIgnoreCase)
            .Paginate(request);

        return TypedResults.Ok(paginatedRoster);
    }
}
