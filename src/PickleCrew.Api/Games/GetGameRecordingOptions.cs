using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Games;

public sealed record GetGameRecordingOptionsHttpResponse
{
    public required GameRecordingMemberHttpResponse[] Members { get; init; }
}

public sealed record GameRecordingMemberHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed class GetGameRecordingOptionsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/recording-options", HandleAsync)
            .WithName("GetGameRecordingOptions")
            .WithSummary("Gets active crew members available for game recording.");
    }

    private static async Task<Results<
        Ok<GetGameRecordingOptionsHttpResponse>,
        ForbidHttpResult>> HandleAsync(
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var activeMembers = await workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );

        var recorderIsActive = activeMembers.Any(
            member => member.WorkOsUserId == crewUserContext.UserId
        );

        if (!recorderIsActive)
        {
            return TypedResults.Forbid();
        }

        var response = new GetGameRecordingOptionsHttpResponse
        {
            Members = activeMembers
                .OrderBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(member => new GameRecordingMemberHttpResponse
                {
                    UserId = member.WorkOsUserId,
                    DisplayName = member.DisplayName,
                    ProfilePictureUrl = member.ProfilePictureUrl,
                })
                .ToArray(),
        };

        return TypedResults.Ok(response);
    }
}
