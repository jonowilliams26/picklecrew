using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetBrickWallHttpResponse
{
    public required BrickWallHttpResponse? BrickWall { get; init; }
}

public sealed record BrickWallHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int GamesPlayed { get; init; }

    public required int PointsConceded { get; init; }

    public required decimal AveragePointsConceded { get; init; }
}

internal sealed class GetBrickWallEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/brick-wall", HandleAsync)
            .WithName("GetBrickWall")
            .WithSummary("Gets the Brick Wall for the active Crew.");
    }

    private static async Task<Ok<GetBrickWallHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.BrickWall
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

    private static async ValueTask<GetBrickWallHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var brickWallStatistics = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
                PointsConceded = group.Sum(statistics => statistics.PointsConceded),
                AveragePointsConceded = (decimal)group.Sum(
                    statistics => statistics.PointsConceded
                ) / group.Sum(statistics => statistics.Wins + statistics.Losses),
            })
            .OrderBy(statistics => statistics.AveragePointsConceded)
            .ThenBy(statistics => statistics.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (brickWallStatistics is null)
        {
            return new GetBrickWallHttpResponse
            {
                BrickWall = null,
            };
        }

        var member = activeMembersByUserId[brickWallStatistics.UserId];

        return new GetBrickWallHttpResponse
        {
            BrickWall = new BrickWallHttpResponse
            {
                UserId = member.WorkOsUserId,
                DisplayName = member.DisplayName,
                ProfilePictureUrl = member.ProfilePictureUrl,
                GamesPlayed = brickWallStatistics.GamesPlayed,
                PointsConceded = brickWallStatistics.PointsConceded,
                AveragePointsConceded = brickWallStatistics.AveragePointsConceded,
            },
        };
    }
}
