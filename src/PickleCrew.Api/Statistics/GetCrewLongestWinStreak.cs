using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

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
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var candidates = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                activeMemberUserIds.Contains(statistics.UserId)
                && statistics.LongestWinStreak > 0
            )
            .Select(statistics => new
            {
                statistics.UserId,
                statistics.LongestWinStreak,
                statistics.LongestWinStreakStartedAtUtc,
                statistics.LongestWinStreakEndedAtUtc,
            })
            .ToArrayAsync(cancellationToken);

        var recordHolder = candidates
            .Select(candidate => new
            {
                Member = activeMembersByUserId[candidate.UserId],
                candidate.LongestWinStreak,
                candidate.LongestWinStreakStartedAtUtc,
                candidate.LongestWinStreakEndedAtUtc,
            })
            .OrderByDescending(candidate => candidate.LongestWinStreak)
            .ThenBy(candidate =>
                candidate.LongestWinStreakStartedAtUtc ?? DateTimeOffset.MaxValue
            )
            .ThenBy(
                candidate => candidate.Member.DisplayName,
                StringComparer.OrdinalIgnoreCase
            )
            .ThenBy(
                candidate => candidate.Member.WorkOsUserId,
                StringComparer.Ordinal
            )
            .FirstOrDefault();

        return TypedResults.Ok(new GetCrewLongestWinStreakHttpResponse
        {
            UserId = recordHolder?.Member.WorkOsUserId,
            DisplayName = recordHolder?.Member.DisplayName,
            ProfilePictureUrl = recordHolder?.Member.ProfilePictureUrl,
            LongestWinStreak = recordHolder?.LongestWinStreak ?? 0,
            LongestWinStreakStartedAtUtc = recordHolder?.LongestWinStreakStartedAtUtc,
            LongestWinStreakEndedAtUtc = recordHolder?.LongestWinStreakEndedAtUtc,
        });
    }
}
