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

    public required long GamesPlayed { get; init; }
}

public sealed record RivalPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required decimal WinPercentage { get; init; }
}

internal sealed class GetBiggestRivalsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/biggest-rivals", HandleAsync)
            .WithName("GetBiggestRivals")
            .WithSummary("Gets the biggest rivals for the active Crew.");
    }

    private static async Task<Ok<GetBiggestRivalsHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.BiggestRivals
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

    private static async ValueTask<GetBiggestRivalsHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeUserIds = activeMembersByUserId.Keys;

        var rivalry = await database.CrewMatchupStatistics
            .AsNoTracking()
            .Where(matchup =>
                activeUserIds.Contains(matchup.UserId)
                && activeUserIds.Contains(matchup.OpponentUserId)
                && matchup.GamesPlayed >= 3
            )
            .OrderBy(matchup => Math.Abs(matchup.WinPercentage - 0.5m))
            .ThenByDescending(matchup => matchup.GamesPlayed)
            .ThenBy(matchup => matchup.UserId)
            .ThenBy(matchup => matchup.OpponentUserId)
            .Select(matchup => new RivalryCandidate
            {
                UserId = matchup.UserId,
                OpponentUserId = matchup.OpponentUserId,
                Wins = matchup.Wins,
                Losses = matchup.Losses,
                GamesPlayed = matchup.GamesPlayed,
                WinPercentage = matchup.WinPercentage,
            })
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
        long wins,
        long losses,
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

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }
}
