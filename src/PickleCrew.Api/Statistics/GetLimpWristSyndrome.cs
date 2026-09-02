using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetLimpWristSyndromeHttpResponse
{
    public required LimpWristSyndromeHttpResponse? LimpWristSyndrome { get; init; }
}

public sealed record LimpWristSyndromeHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int Losses { get; init; }

    public required int PointDifferential { get; init; }
}

internal sealed class GetLimpWristSyndromeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/limp-wrist-syndrome", HandleAsync)
            .WithName("GetLimpWristSyndrome")
            .WithSummary("Gets the limp wrist syndrome holder for the active Crew.");
    }

    private static async Task<Ok<GetLimpWristSyndromeHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.LimpWristSyndrome
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

    private static async ValueTask<GetLimpWristSyndromeHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var holderStatistics = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Losses = group.Sum(statistics => statistics.Losses),
                PointDifferential = group.Sum(statistics => statistics.PointDifferential),
            })
            .Where(statistics => statistics.Losses > 0)
            .OrderByDescending(statistics => statistics.Losses)
            .ThenBy(statistics => statistics.PointDifferential)
            .ThenBy(statistics => statistics.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (holderStatistics is null)
        {
            return new GetLimpWristSyndromeHttpResponse
            {
                LimpWristSyndrome = null,
            };
        }

        var holder = activeMembersByUserId[holderStatistics.UserId];

        return new GetLimpWristSyndromeHttpResponse
        {
            LimpWristSyndrome = new LimpWristSyndromeHttpResponse
            {
                UserId = holder.WorkOsUserId,
                DisplayName = holder.DisplayName,
                ProfilePictureUrl = holder.ProfilePictureUrl,
                Losses = holderStatistics.Losses,
                PointDifferential = holderStatistics.PointDifferential,
            },
        };
    }
}
