using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetHotPaddleHttpResponse
{
    public required HotPaddleHttpResponse? HotPaddle { get; init; }
}

public sealed record HotPaddleHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }
}

internal sealed class GetHotPaddleEndpoint : IEndpoint
{
    private const int RecentGameLimit = 5;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/hot-paddle", HandleAsync)
            .WithName("GetHotPaddle")
            .WithSummary("Gets the active Crew member with the best form over their last five Games.");
    }

    private static async Task<Ok<GetHotPaddleHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.HotPaddle
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

    private static async ValueTask<GetHotPaddleHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var hotPaddleStatistics = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new HotPaddleStatistics
            {
                UserId = group.Key,
                Wins = group
                    .OrderByDescending(statistics => statistics.PlayedAtUtc)
                    .ThenByDescending(statistics => statistics.GameId)
                    .Take(RecentGameLimit)
                    .Sum(statistics => statistics.Wins),
                GamesPlayed = group
                    .OrderByDescending(statistics => statistics.PlayedAtUtc)
                    .ThenByDescending(statistics => statistics.GameId)
                    .Take(RecentGameLimit)
                    .Count(),
                PointDifferential = group
                    .OrderByDescending(statistics => statistics.PlayedAtUtc)
                    .ThenByDescending(statistics => statistics.GameId)
                    .Take(RecentGameLimit)
                    .Sum(statistics => statistics.PointDifferential),
            })
            .Where(statistics => statistics.GamesPlayed == RecentGameLimit)
            .OrderByDescending(statistics => statistics.Wins)
            .ThenByDescending(statistics => statistics.PointDifferential)
            .FirstOrDefaultAsync(cancellationToken);

        if (hotPaddleStatistics is null)
        {
            return new GetHotPaddleHttpResponse
            {
                HotPaddle = null,
            };
        }

        var hotPaddle = activeMembersByUserId[hotPaddleStatistics.UserId];
        var losses = hotPaddleStatistics.GamesPlayed - hotPaddleStatistics.Wins;

        return new GetHotPaddleHttpResponse
        {
            HotPaddle = new HotPaddleHttpResponse
            {
                UserId = hotPaddle.WorkOsUserId,
                DisplayName = hotPaddle.DisplayName,
                ProfilePictureUrl = hotPaddle.ProfilePictureUrl,
                Wins = hotPaddleStatistics.Wins,
                Losses = losses,
                GamesPlayed = hotPaddleStatistics.GamesPlayed,
                WinPercentage = (decimal)hotPaddleStatistics.Wins
                    / hotPaddleStatistics.GamesPlayed,
            },
        };
    }
}

internal sealed record HotPaddleStatistics
{
    public required string UserId { get; init; }

    public required int Wins { get; init; }

    public required int GamesPlayed { get; init; }

    public required int PointDifferential { get; init; }
}
