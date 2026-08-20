using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Games.Services;
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
            .WithSummary("Gets active crew members available for game recording.")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<Results<
        Ok<GetGameRecordingOptionsHttpResponse>,
        ProblemHttpResult>> HandleAsync(
        CrewUserContext crewUserContext,
        IWorkOsService workOs,
        CancellationToken cancellationToken)
    {
        var activeMembers = await ActiveCrewRoster.TryListAsync(
            workOs,
            crewUserContext.OrganizationId,
            cancellationToken
        );

        if (activeMembers is null)
        {
            return TypedResults.Problem(
                detail: "Crew members could not be loaded. Try again.",
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Crew members unavailable"
            );
        }

        var recorderIsActive = activeMembers.Any(
            member => member.WorkOsUserId == crewUserContext.UserId
        );

        if (!recorderIsActive)
        {
            return TypedResults.Problem(
                detail: "Your active Crew membership could not be verified.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Crew membership required"
            );
        }

        var response = new GetGameRecordingOptionsHttpResponse
        {
            Members = activeMembers
                .OrderBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(ToHttpResponse)
                .ToArray(),
        };

        return TypedResults.Ok(response);
    }

    private static GameRecordingMemberHttpResponse ToHttpResponse(
        ActiveCrewMember member)
    {
        return new GameRecordingMemberHttpResponse
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
        };
    }
}
