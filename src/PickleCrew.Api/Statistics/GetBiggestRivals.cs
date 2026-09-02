using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetBiggestRivalsHttpResponse
{
    public required BiggestRivalryHttpResponse? Rivalry { get; init; }
}

public sealed record BiggestRivalryHttpResponse
{
    public required RivalPlayerHttpResponse[] Players { get; init; }

    public required int GamesPlayed { get; init; }
}

public sealed record RivalPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required decimal WinPercentage { get; init; }
}

internal sealed class GetBiggestRivalsEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/biggest-rivals", HandleAsync)
            .WithName("GetBiggestRivals")
            .WithSummary("Gets the biggest rivals for the active Crew.");
    }

    private static async Task<Ok<GetBiggestRivalsHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.BiggestRivals
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

    private static async ValueTask<GetBiggestRivalsHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeUserIds = activeMembersByUserId.Keys.ToArray();

        var rivalry = await database.CrewMatchupGameStatistics
            .Where(statistics =>
                activeUserIds.Contains(statistics.UserId)
                && activeUserIds.Contains(statistics.OpponentUserId)
            )
            .GroupBy(statistics => new
            {
                statistics.UserId,
                statistics.OpponentUserId,
            })
            .Select(group => new RivalryCandidate
            {
                UserId = group.Key.UserId,
                OpponentUserId = group.Key.OpponentUserId,
                Wins = group.Sum(statistics => statistics.Wins),
                Losses = group.Sum(statistics => statistics.Losses),
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
                WinPercentage = (decimal)group.Sum(statistics => statistics.Wins)
                    / group.Sum(statistics => statistics.Wins + statistics.Losses),
                PointDifferential = group.Sum(statistics => statistics.PointDifferential),
            })
            .Where(matchup => matchup.GamesPlayed >= MinimumGamesPlayed)
            .OrderBy(matchup => Math.Abs(matchup.WinPercentage - 0.5m))
            .ThenByDescending(matchup => matchup.GamesPlayed)
            .ThenBy(matchup => matchup.UserId)
            .ThenBy(matchup => matchup.OpponentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (rivalry is null)
        {
            return new GetBiggestRivalsHttpResponse
            {
                Rivalry = null,
            };
        }

        var players = new[]
        {
            CreatePlayer(
                activeMembersByUserId[rivalry.UserId],
                rivalry.Wins,
                rivalry.Losses,
                rivalry.WinPercentage
            ),
            CreatePlayer(
                activeMembersByUserId[rivalry.OpponentUserId],
                rivalry.Losses,
                rivalry.Wins,
                1 - rivalry.WinPercentage
            ),
        }
            .OrderBy(player => player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.UserId, StringComparer.Ordinal)
            .ToArray();

        return new GetBiggestRivalsHttpResponse
        {
            Rivalry = new BiggestRivalryHttpResponse
            {
                Players = players,
                GamesPlayed = rivalry.GamesPlayed,
            },
        };
    }

    private static RivalPlayerHttpResponse CreatePlayer(
        ActiveCrewMember member,
        int wins,
        int losses,
        decimal winPercentage)
    {
        return new RivalPlayerHttpResponse
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
            Wins = wins,
            Losses = losses,
            WinPercentage = winPercentage,
        };
    }
}

internal sealed record RivalryCandidate
{
    public required string UserId { get; init; }

    public required string OpponentUserId { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointDifferential { get; init; }
}
