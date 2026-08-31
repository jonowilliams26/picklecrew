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
        var candidates = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                activeMemberUserIds.Contains(statistics.UserId)
                && statistics.ThatsAPaddlin > 0
            )
            .Select(statistics => new
            {
                statistics.UserId,
                statistics.ThatsAPaddlin,
            })
            .ToArrayAsync(cancellationToken);

        var chiefPaddler = candidates
            .Select(candidate => new
            {
                Member = activeMembersByUserId[candidate.UserId],
                candidate.ThatsAPaddlin,
            })
            .OrderByDescending(candidate => candidate.ThatsAPaddlin)
            .ThenBy(
                candidate => candidate.Member.DisplayName,
                StringComparer.OrdinalIgnoreCase
            )
            .ThenBy(
                candidate => candidate.Member.WorkOsUserId,
                StringComparer.Ordinal
            )
            .FirstOrDefault();

        return TypedResults.Ok(new GetChiefPaddlerHttpResponse
        {
            ChiefPaddler = chiefPaddler is null
                ? null
                : new ChiefPaddlerHttpResponse
                {
                    UserId = chiefPaddler.Member.WorkOsUserId,
                    DisplayName = chiefPaddler.Member.DisplayName,
                    ProfilePictureUrl = chiefPaddler.Member.ProfilePictureUrl,
                    ThatsAPaddlin = chiefPaddler.ThatsAPaddlin,
                },
        });
    }
}
