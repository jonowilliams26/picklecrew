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

    public required long Wins { get; init; }

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
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.CrewLongestWinStreak
            ),
            crewUserContext,
            scopeFactory,
            static (services, factoryCancellationToken) => GetResponseAsync(
                services.GetRequiredService<CrewScopedDbContext>(),
                services.GetRequiredService<ICrewService>(),
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
        var recordHolderStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics =>
                activeMemberUserIds.Contains(statistics.UserId)
                && statistics.LongestWinStreak > 0
            )
            .OrderByDescending(statistics => statistics.LongestWinStreak)
            .ThenByDescending(statistics => statistics.LongestWinStreakStartedAtUtc)
            .Select(statistics => new
            {
                statistics.UserId,
                statistics.LongestWinStreak,
                statistics.LongestWinStreakStartedAtUtc,
                statistics.LongestWinStreakEndedAtUtc,
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
