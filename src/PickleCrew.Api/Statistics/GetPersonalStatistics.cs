using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetPersonalStatisticsHttpResponse
{
    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required int ThatsAPaddlin { get; init; }

    public required int CurrentWinStreak { get; init; }

    public required DateTimeOffset? CurrentWinStreakStartedAtUtc { get; init; }

    public required int LongestWinStreak { get; init; }

    public required DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public required DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }

    public required decimal AverageMargin { get; init; }

    public required int CrewScore { get; init; }

    public required int CrewRank { get; init; }

    public required int ActiveCrewMemberCount { get; init; }
}

internal sealed class GetPersonalStatisticsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/personal", HandleAsync)
            .WithName("GetPersonalStatistics")
            .WithSummary("Gets the signed-in user's all-time statistics for the active Crew.");
    }

    private static async Task<Ok<GetPersonalStatisticsHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewStandingsService standingsService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new PersonalStatisticsCacheOption(
                crewUserContext.CrewUuid,
                crewUserContext.UserId,
                PersonalStatisticsCacheResource.PersonalStatistics
            ),
            factoryCancellationToken => GetResponseAsync(
                crewUserContext.UserId,
                standingsService,
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetPersonalStatisticsHttpResponse> GetResponseAsync(
        string userId,
        CrewStandingsService standingsService,
        CancellationToken cancellationToken)
    {
        var standing = await standingsService.GetMemberStandingAsync(
            userId,
            cancellationToken
        );

        return new GetPersonalStatisticsHttpResponse
        {
            Wins = standing.Wins,
            Losses = standing.Losses,
            GamesPlayed = standing.GamesPlayed,
            ThatsAPaddlin = standing.ThatsAPaddlin,
            CurrentWinStreak = standing.CurrentWinStreak,
            CurrentWinStreakStartedAtUtc = standing.CurrentWinStreakStartedAtUtc,
            LongestWinStreak = standing.LongestWinStreak,
            LongestWinStreakStartedAtUtc = standing.LongestWinStreakStartedAtUtc,
            LongestWinStreakEndedAtUtc = standing.LongestWinStreakEndedAtUtc,
            WinPercentage = standing.WinPercentage,
            PointsScored = standing.PointsScored,
            PointsConceded = standing.PointsConceded,
            PointDifferential = standing.PointDifferential,
            AverageMargin = standing.AverageMargin,
            CrewScore = standing.CrewScore,
            CrewRank = standing.Rank,
            ActiveCrewMemberCount = standing.ActiveCrewMemberCount,
        };
    }
}
