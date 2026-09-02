using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetNemesisHttpResponse
{
    public required NemesisHttpResponse? Nemesis { get; init; }
}

public sealed record NemesisHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int Losses { get; init; }
}

internal sealed class GetNemesisEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/nemesis", HandleAsync)
            .WithName("GetNemesis")
            .WithSummary("Gets the signed-in user's Nemesis for the active Crew.");
    }

    private static async Task<Ok<GetNemesisHttpResponse>> HandleAsync(
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
                PersonalStatisticsCacheResource.Nemesis
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

    private static async ValueTask<GetNemesisHttpResponse> GetResponseAsync(
        string userId,
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberIds = activeMembersByUserId.Keys.ToArray();
        var matchup = await database.CrewMatchupGameStatistics
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
                Losses = group.Sum(statistics => statistics.Losses),
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
            })
            .Where(statistics =>
                statistics.Losses > 0
                && statistics.GamesPlayed >= MinimumGamesPlayed
            )
            .OrderByDescending(statistics => statistics.Losses)
            .ThenBy(statistics => statistics.OpponentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (matchup is null)
        {
            return new GetNemesisHttpResponse
            {
                Nemesis = null,
            };
        }

        var opponent = activeMembersByUserId[matchup.OpponentUserId];

        return new GetNemesisHttpResponse
        {
            Nemesis = new NemesisHttpResponse
            {
                UserId = opponent.WorkOsUserId,
                DisplayName = opponent.DisplayName,
                ProfilePictureUrl = opponent.ProfilePictureUrl,
                Losses = matchup.Losses,
            },
        };
    }
}
