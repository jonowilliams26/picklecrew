using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetEasyWinHttpResponse
{
    public required EasyWinHttpResponse? EasyWin { get; init; }
}

public sealed record EasyWinHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int Wins { get; init; }
}

internal sealed class GetEasyWinEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/easy-win", HandleAsync)
            .WithName("GetEasyWin")
            .WithSummary("Gets the signed-in user's Easy Win for the active Crew.");
    }

    private static async Task<Ok<GetEasyWinHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new PersonalStatisticsCacheOption(
                crewUserContext.CrewUuid,
                crewUserContext.UserId,
                PersonalStatisticsCacheResource.EasyWin
            ),
            factoryCancellationToken => GetResponseAsync(
                crewUserContext.UserId,
                database,
                crewService,
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetEasyWinHttpResponse> GetResponseAsync(
        string userId,
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberIds = activeMembersByUserId.Keys.ToArray();
        var matchup = await database.MatchupStatistics
            .Where(statistics =>
                statistics.UserId == userId
                && activeMemberIds.Contains(statistics.OpponentUserId)
            )
            .GroupBy(statistics => new
            {
                statistics.UserId,
                statistics.OpponentUserId,
            })
            .Select(group => new
            {
                group.Key.OpponentUserId,
                Wins = group.Sum(statistics => statistics.Wins),
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
            })
            .Where(statistics =>
                statistics.Wins > 0
                && statistics.GamesPlayed >= MinimumGamesPlayed
            )
            .OrderByDescending(statistics => statistics.Wins)
            .ThenBy(statistics => statistics.OpponentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (matchup is null)
        {
            return new GetEasyWinHttpResponse
            {
                EasyWin = null,
            };
        }

        var opponent = activeMembersByUserId[matchup.OpponentUserId];

        return new GetEasyWinHttpResponse
        {
            EasyWin = new EasyWinHttpResponse
            {
                UserId = opponent.WorkOsUserId,
                DisplayName = opponent.DisplayName,
                ProfilePictureUrl = opponent.ProfilePictureUrl,
                Wins = matchup.Wins,
            },
        };
    }
}
