using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
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

    public required int ThatsAPaddlin { get; init; }
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
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.ChiefPaddler
            ),
            factoryCancellationToken => GetResponseAsync(
                database,
                crewService,
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetChiefPaddlerHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var chiefPaddlerStatistics = await database.CrewMemberGameStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                ThatsAPaddlin = group.Sum(statistics => statistics.ThatsAPaddlin),
                CrewScore = group.Sum(statistics => statistics.CrewScore),
            })
            .Where(statistics => statistics.ThatsAPaddlin > 0)
            .OrderByDescending(statistics => statistics.ThatsAPaddlin)
            .ThenByDescending(statistics => statistics.CrewScore)
            .ThenBy(statistics => statistics.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (chiefPaddlerStatistics is null)
        {
            return new GetChiefPaddlerHttpResponse
            {
                ChiefPaddler = null,
            };
        }

        var chiefPaddler = activeMembersByUserId[chiefPaddlerStatistics.UserId];

        return new GetChiefPaddlerHttpResponse
        {
            ChiefPaddler = new ChiefPaddlerHttpResponse
            {
                UserId = chiefPaddler.WorkOsUserId,
                DisplayName = chiefPaddler.DisplayName,
                ProfilePictureUrl = chiefPaddler.ProfilePictureUrl,
                ThatsAPaddlin = chiefPaddlerStatistics.ThatsAPaddlin,
            },
        };
    }
}
