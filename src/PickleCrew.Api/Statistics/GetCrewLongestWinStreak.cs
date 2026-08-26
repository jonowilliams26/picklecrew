using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetCrewLongestWinStreakHttpResponse
{
    public required string? UserId { get; init; }

    public required string? DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required long LongestWinStreak { get; init; }

    public required DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public required DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }
}

internal sealed class GetCrewLongestWinStreakEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/longest-win-streak", HandleAsync)
            .WithName("GetCrewLongestWinStreak")
            .WithSummary("Gets the longest win streak for the active Crew.");
    }

    private static async Task<Ok<GetCrewLongestWinStreakHttpResponse>> HandleAsync(
        CrewStandingsService standingsService,
        CancellationToken cancellationToken)
    {
        var standings = await standingsService.GetStandingsAsync(cancellationToken);
        var recordHolder = standings
            .Where(standing => standing.LongestWinStreak > 0)
            .OrderByDescending(standing => standing.LongestWinStreak)
            .ThenBy(standing =>
                standing.LongestWinStreakStartedAtUtc ?? DateTimeOffset.MaxValue
            )
            .ThenBy(standing => standing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(standing => standing.UserId, StringComparer.Ordinal)
            .FirstOrDefault();

        return TypedResults.Ok(new GetCrewLongestWinStreakHttpResponse
        {
            UserId = recordHolder?.UserId,
            DisplayName = recordHolder?.DisplayName,
            ProfilePictureUrl = recordHolder?.ProfilePictureUrl,
            LongestWinStreak = recordHolder?.LongestWinStreak ?? 0,
            LongestWinStreakStartedAtUtc = recordHolder?.LongestWinStreakStartedAtUtc,
            LongestWinStreakEndedAtUtc = recordHolder?.LongestWinStreakEndedAtUtc,
        });
    }
}
