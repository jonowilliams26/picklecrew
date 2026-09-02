using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetCrewLongestWinStreakHttpResponse
{
    public required LongestWinStreakHttpResponse? LongestWinStreak { get; init; }
}

public sealed record LongestWinStreakHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required int Wins { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset? EndedAtUtc { get; init; }
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
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.CrewLongestWinStreak
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

    private static async ValueTask<GetCrewLongestWinStreakHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var recordHolderStatistics = await database.CrewMemberWinStreaks
            .AsNoTracking()
            .Where(winStreak =>
                activeMemberUserIds.Contains(winStreak.UserId)
                && winStreak.LongestWinStreak > 0
            )
            .OrderByDescending(winStreak => winStreak.LongestWinStreak)
            .ThenByDescending(winStreak => winStreak.LongestWinStreakStartedAtUtc)
            .Select(winStreak => new
            {
                winStreak.UserId,
                winStreak.LongestWinStreak,
                winStreak.LongestWinStreakStartedAtUtc,
                winStreak.LongestWinStreakEndedAtUtc,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (recordHolderStatistics is null)
        {
            return new GetCrewLongestWinStreakHttpResponse
            {
                LongestWinStreak = null,
            };
        }

        var recordHolder = activeMembersByUserId[recordHolderStatistics.UserId];

        return new GetCrewLongestWinStreakHttpResponse
        {
            LongestWinStreak = new LongestWinStreakHttpResponse
            {
                UserId = recordHolder.WorkOsUserId,
                DisplayName = recordHolder.DisplayName,
                ProfilePictureUrl = recordHolder.ProfilePictureUrl,
                Wins = recordHolderStatistics.LongestWinStreak,
                StartedAtUtc = recordHolderStatistics.LongestWinStreakStartedAtUtc!.Value,
                EndedAtUtc = recordHolderStatistics.LongestWinStreakEndedAtUtc,
            },
        };
    }
}
