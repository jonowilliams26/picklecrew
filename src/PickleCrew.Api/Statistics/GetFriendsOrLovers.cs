using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetFriendsOrLoversHttpResponse
{
    public required FriendsOrLoversPairingHttpResponse? Pairing { get; init; }
}

public sealed record FriendsOrLoversPairingHttpResponse
{
    public required FriendsOrLoversPlayerHttpResponse[] Players { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointDifferential { get; init; }
}

public sealed record FriendsOrLoversPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }
}

internal sealed class GetFriendsOrLoversEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/friends-or-lovers", HandleAsync)
            .WithName("GetFriendsOrLovers")
            .WithSummary("Gets the active Crew pairing that has played the most Games together.");
    }

    private static async Task<Ok<GetFriendsOrLoversHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.FriendsOrLovers
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

    private static async ValueTask<GetFriendsOrLoversHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeUserIds = activeMembersByUserId.Keys.ToArray();

        var statistics = await database.DoublesPairingStatistics
            .Where(candidate =>
                activeUserIds.Contains(candidate.Player1Id)
                && activeUserIds.Contains(candidate.Player2Id)
            )
            .GroupBy(candidate => new
            {
                candidate.Player1Id,
                candidate.Player2Id,
            })
            .Select(group => new FriendsOrLoversStatisticsTotals
            {
                Player1Id = group.Key.Player1Id,
                Player2Id = group.Key.Player2Id,
                Wins = group.Sum(candidate => candidate.Wins),
                Losses = group.Sum(candidate => candidate.Losses),
                GamesPlayed = group.Sum(candidate => candidate.Wins + candidate.Losses),
                WinPercentage = (decimal)group.Sum(candidate => candidate.Wins)
                    / group.Sum(candidate => candidate.Wins + candidate.Losses),
                PointDifferential = group.Sum(candidate => candidate.PointDifferential),
            })
            .Where(candidate => candidate.GamesPlayed >= 3)
            .OrderByDescending(candidate => candidate.GamesPlayed)
            .ThenByDescending(candidate => candidate.Wins)
            .ThenByDescending(candidate => candidate.PointDifferential)
            .FirstOrDefaultAsync(cancellationToken);

        if (statistics is null)
        {
            return new GetFriendsOrLoversHttpResponse
            {
                Pairing = null,
            };
        }

        var players = new[]
        {
            CreatePlayer(activeMembersByUserId[statistics.Player1Id]),
            CreatePlayer(activeMembersByUserId[statistics.Player2Id]),
        }
            .OrderBy(player => player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.UserId, StringComparer.Ordinal)
            .ToArray();

        return new GetFriendsOrLoversHttpResponse
        {
            Pairing = new FriendsOrLoversPairingHttpResponse
            {
                Players = players,
                Wins = statistics.Wins,
                Losses = statistics.Losses,
                GamesPlayed = statistics.GamesPlayed,
                WinPercentage = statistics.WinPercentage,
                PointDifferential = statistics.PointDifferential,
            },
        };
    }

    private static FriendsOrLoversPlayerHttpResponse CreatePlayer(
        ActiveCrewMember member)
    {
        return new FriendsOrLoversPlayerHttpResponse
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
        };
    }
}

internal sealed record FriendsOrLoversStatisticsTotals
{
    public required string Player1Id { get; init; }

    public required string Player2Id { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointDifferential { get; init; }
}
