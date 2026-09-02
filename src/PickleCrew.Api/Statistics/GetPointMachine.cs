using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetPointMachineHttpResponse
{
    public required PointMachineHttpResponse? PointMachine { get; init; }
}

public sealed record PointMachineHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int PointsScored { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal AveragePointsScored { get; init; }
}

internal sealed class GetPointMachineEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/point-machine", HandleAsync)
            .WithName("GetPointMachine")
            .WithSummary("Gets the Point Machine for the active Crew.");
    }

    private static async Task<Ok<GetPointMachineHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.PointMachine
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

    private static async ValueTask<GetPointMachineHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var pointMachineStatistics = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                PointsScored = group.Sum(statistics => statistics.PointsScored),
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
                AveragePointsScored = (decimal)group.Sum(
                    statistics => statistics.PointsScored
                ) / group.Sum(statistics => statistics.Wins + statistics.Losses),
            })
            .Where(statistics => statistics.GamesPlayed >= MinimumGamesPlayed)
            .OrderByDescending(statistics => statistics.AveragePointsScored)
            .ThenByDescending(statistics => statistics.GamesPlayed)
            .ThenBy(statistics => statistics.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (pointMachineStatistics is null)
        {
            return new GetPointMachineHttpResponse
            {
                PointMachine = null,
            };
        }

        var pointMachine = activeMembersByUserId[pointMachineStatistics.UserId];

        return new GetPointMachineHttpResponse
        {
            PointMachine = new PointMachineHttpResponse
            {
                UserId = pointMachine.WorkOsUserId,
                DisplayName = pointMachine.DisplayName,
                ProfilePictureUrl = pointMachine.ProfilePictureUrl,
                PointsScored = pointMachineStatistics.PointsScored,
                GamesPlayed = pointMachineStatistics.GamesPlayed,
                AveragePointsScored = pointMachineStatistics.AveragePointsScored,
            },
        };
    }
}
