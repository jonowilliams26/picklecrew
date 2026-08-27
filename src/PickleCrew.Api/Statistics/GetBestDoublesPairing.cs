using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }
}

public sealed record DoublesPairingPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed class GetBestDoublesPairingEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/best-doubles-pairing", HandleAsync)
            .WithName("GetBestDoublesPairing")
            .WithSummary("Gets the best doubles pairing for the active Crew.");
    }

    private static async Task<Ok<GetBestDoublesPairingHttpResponse>> HandleAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembers = await crewService.ListActiveCrewMembersAsync(cancellationToken);
        var activeMembersByUserId = activeMembers.ToDictionary(
            member => member.WorkOsUserId,
            StringComparer.Ordinal
        );
        var activeUserIds = activeMembersByUserId.Keys.ToArray();

        var statistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(candidate =>
                activeUserIds.Contains(candidate.Player1Id)
                && activeUserIds.Contains(candidate.Player2Id)
            )
            .Select(candidate => new DoublesPairingStatisticsTotals
            {
                Player1Id = candidate.Player1Id,
                Player2Id = candidate.Player2Id,
                Wins = candidate.Wins,
                Losses = candidate.Losses,
                GamesPlayed = candidate.Wins + candidate.Losses,
                WinPercentage = (decimal)candidate.Wins
                    / (candidate.Wins + candidate.Losses),
                PointsScored = candidate.PointsScored,
                PointsConceded = candidate.PointsConceded,
                PointDifferential = candidate.PointsScored - candidate.PointsConceded,
            })
            .OrderByDescending(candidate => candidate.WinPercentage)
            .ThenByDescending(candidate => candidate.GamesPlayed)
            .ThenByDescending(candidate => candidate.PointDifferential)
            .FirstOrDefaultAsync(cancellationToken);

        if (statistics is null)
        {
            return TypedResults.Ok(new GetBestDoublesPairingHttpResponse
            {
                Pairing = null,
            });
        }

        var players = new[]
        {
            CreatePlayer(activeMembersByUserId[statistics.Player1Id]),
            CreatePlayer(activeMembersByUserId[statistics.Player2Id]),
        }
            .OrderBy(player => player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.UserId, StringComparer.Ordinal)
            .ToArray();

        return TypedResults.Ok(new GetBestDoublesPairingHttpResponse
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
        });
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

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }
}
