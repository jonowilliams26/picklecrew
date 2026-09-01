using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetCrewLeaderboardHttpResponse
{
    public required CrewLeaderboardEntryHttpResponse[] Entries { get; init; }
}

public sealed record CrewLeaderboardEntryHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required int Rank { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long CrewScore { get; init; }
}

internal sealed class GetCrewLeaderboardEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/leaderboard", HandleAsync)
            .WithName("GetCrewLeaderboard")
            .WithSummary("Gets the complete leaderboard for the active Crew.");
    }

    private static async Task<Ok<GetCrewLeaderboardHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.CrewLeaderboard
            ),
            crewUserContext,
            scopeFactory,
            static (services, factoryCancellationToken) => GetResponseAsync(
                services.GetRequiredService<CrewStandingsService>(),
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetCrewLeaderboardHttpResponse> GetResponseAsync(
        CrewStandingsService standingsService,
        CancellationToken cancellationToken)
    {
        var standings = await standingsService.GetStandingsAsync(cancellationToken);

        return new GetCrewLeaderboardHttpResponse
        {
            Entries = standings
                .Select(standing => new CrewLeaderboardEntryHttpResponse
                {
                    UserId = standing.UserId,
                    DisplayName = standing.DisplayName,
                    ProfilePictureUrl = standing.ProfilePictureUrl,
                    Rank = standing.Rank,
                    Wins = standing.Wins,
                    Losses = standing.Losses,
                    CrewScore = standing.CrewScore,
                })
                .ToArray(),
        };
    }
}
