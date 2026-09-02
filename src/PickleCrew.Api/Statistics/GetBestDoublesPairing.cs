using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetBestDoublesPairingHttpResponse
{
    public required DoublesPairingHttpResponse? Pairing { get; init; }
}

public sealed record DoublesPairingHttpResponse
{
    public required DoublesPairingPlayerHttpResponse[] Players { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }
}

public sealed record DoublesPairingPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed class GetBestDoublesPairingEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/best-doubles-pairing", HandleAsync)
            .WithName("GetBestDoublesPairing")
            .WithSummary("Gets the best doubles pairing for the active Crew.");
    }

    private static async Task<Ok<GetBestDoublesPairingHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.BestDoublesPairing
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

    private static async ValueTask<GetBestDoublesPairingHttpResponse> GetResponseAsync(
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
            .Select(group => new DoublesPairingStatisticsTotals
            {
                Player1Id = group.Key.Player1Id,
                Player2Id = group.Key.Player2Id,
                Wins = group.Sum(candidate => candidate.Wins),
                Losses = group.Sum(candidate => candidate.Losses),
                GamesPlayed = group.Sum(candidate => candidate.Wins + candidate.Losses),
                WinPercentage = (decimal)group.Sum(candidate => candidate.Wins)
                    / group.Sum(candidate => candidate.Wins + candidate.Losses),
                PointsScored = group.Sum(candidate => candidate.PointsScored),
                PointsConceded = group.Sum(candidate => candidate.PointsConceded),
                PointDifferential = group.Sum(candidate => candidate.PointDifferential),
            })
            .Where(candidate => candidate.GamesPlayed >= MinimumGamesPlayed)
            .OrderByDescending(candidate => candidate.WinPercentage)
            .ThenByDescending(candidate => candidate.GamesPlayed)
            .ThenByDescending(candidate => candidate.PointDifferential)
            .FirstOrDefaultAsync(cancellationToken);

        if (statistics is null)
        {
            return new GetBestDoublesPairingHttpResponse
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

        return new GetBestDoublesPairingHttpResponse
        {
            Pairing = new DoublesPairingHttpResponse
            {
                Players = players,
                Wins = statistics.Wins,
                Losses = statistics.Losses,
                GamesPlayed = statistics.GamesPlayed,
                WinPercentage = statistics.WinPercentage,
                PointsScored = statistics.PointsScored,
                PointsConceded = statistics.PointsConceded,
                PointDifferential = statistics.PointDifferential,
            },
        };
    }

    private static DoublesPairingPlayerHttpResponse CreatePlayer(
        ActiveCrewMember member)
    {
        return new DoublesPairingPlayerHttpResponse
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
        };
    }
}

internal sealed record DoublesPairingStatisticsTotals
{
    public required string Player1Id { get; init; }

    public required string Player2Id { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }
}
