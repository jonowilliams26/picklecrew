using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetPersonalStatisticsHttpResponse
{
    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }

    public required long CrewScore { get; init; }

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
        CancellationToken cancellationToken)
    {
        var standing = await standingsService.GetMemberStandingAsync(
            crewUserContext.UserId,
            cancellationToken
        );

        return TypedResults.Ok(new GetPersonalStatisticsHttpResponse
        {
            Wins = standing.Wins,
            Losses = standing.Losses,
            GamesPlayed = standing.GamesPlayed,
            WinPercentage = standing.WinPercentage,
            PointsScored = standing.PointsScored,
            PointsConceded = standing.PointsConceded,
            PointDifferential = standing.PointDifferential,
            CrewScore = standing.CrewScore,
            CrewRank = standing.Rank,
            ActiveCrewMemberCount = standing.ActiveCrewMemberCount,
        });
    }
}
