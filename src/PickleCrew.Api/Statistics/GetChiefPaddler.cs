using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetChiefPaddlerHttpResponse
{
    public required ChiefPaddlerHttpResponse? ChiefPaddler { get; init; }
}

public sealed record ChiefPaddlerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required long ThatsAPaddlin { get; init; }
}

internal sealed class GetChiefPaddlerEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/chief-paddler", HandleAsync)
            .WithName("GetChiefPaddler")
            .WithSummary("Gets the Chief Paddler for the active Crew.");
    }

    private static async Task<Ok<GetChiefPaddlerHttpResponse>> HandleAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var chiefPaddlerStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                activeMemberUserIds.Contains(statistics.UserId)
                && statistics.ThatsAPaddlin > 0
            )
            .OrderByDescending(statistics => statistics.ThatsAPaddlin)
            .ThenByDescending(statistics => statistics.CrewScore)
            .ThenBy(statistics => statistics.UserId)
            .Select(statistics => new
            {
                statistics.UserId,
                statistics.ThatsAPaddlin,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (chiefPaddlerStatistics is null)
        {
            return TypedResults.Ok(new GetChiefPaddlerHttpResponse
            {
                ChiefPaddler = null,
            });
        }

        var chiefPaddler = activeMembersByUserId[chiefPaddlerStatistics.UserId];

        return TypedResults.Ok(new GetChiefPaddlerHttpResponse
        {
            ChiefPaddler = new ChiefPaddlerHttpResponse
            {
                UserId = chiefPaddler.WorkOsUserId,
                DisplayName = chiefPaddler.DisplayName,
                ProfilePictureUrl = chiefPaddler.ProfilePictureUrl,
                ThatsAPaddlin = chiefPaddlerStatistics.ThatsAPaddlin,
            },
        });
    }
}
