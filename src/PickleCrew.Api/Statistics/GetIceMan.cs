using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetIceManHttpResponse
{
    public required IceManHttpResponse? IceMan { get; init; }
}

public sealed record IceManHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int ClutchWins { get; init; }
}

internal sealed class GetIceManEndpoint : IEndpoint
{
    private const int MinimumOpponentScore = 9;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/ice-man", HandleAsync)
            .WithName("GetIceMan")
            .WithSummary("Gets the Ice Man for the active Crew.");
    }

    private static async Task<Ok<GetIceManHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.IceMan
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

    private static async ValueTask<GetIceManHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var iceManStatistics = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                ClutchWins = group.Sum(statistics =>
                    statistics.PointsConceded >= MinimumOpponentScore
                        ? statistics.Wins
                        : 0
                ),
                CrewScore = group.Sum(statistics => statistics.CrewScore),
            })
            .Where(statistics => statistics.ClutchWins > 0)
            .OrderByDescending(statistics => statistics.ClutchWins)
            .ThenByDescending(statistics => statistics.CrewScore)
            .ThenBy(statistics => statistics.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (iceManStatistics is null)
        {
            return new GetIceManHttpResponse
            {
                IceMan = null,
            };
        }

        var iceMan = activeMembersByUserId[iceManStatistics.UserId];

        return new GetIceManHttpResponse
        {
            IceMan = new IceManHttpResponse
            {
                UserId = iceMan.WorkOsUserId,
                DisplayName = iceMan.DisplayName,
                ProfilePictureUrl = iceMan.ProfilePictureUrl,
                ClutchWins = iceManStatistics.ClutchWins,
            },
        };
    }
}
