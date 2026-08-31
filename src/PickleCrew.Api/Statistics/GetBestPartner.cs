using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetBestPartnerHttpResponse
{
    public required BestPartnerHttpResponse? BestPartner { get; init; }
}

public sealed record BestPartnerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }
}

internal sealed class GetBestPartnerEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/best-partner", HandleAsync)
            .WithName("GetBestPartner")
            .WithSummary("Gets the signed-in user's best doubles partner for the active Crew.");
    }

    private static async Task<Ok<GetBestPartnerHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembers = await crewService.ListActiveCrewMembersAsync(cancellationToken);
        var activePartnersByUserId = activeMembers
            .Where(member => !string.Equals(
                member.WorkOsUserId,
                crewUserContext.UserId,
                StringComparison.Ordinal
            ))
            .ToDictionary(member => member.WorkOsUserId, StringComparer.Ordinal);
        var activePartnerUserIds = activePartnersByUserId.Keys.ToArray();

        var statistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(candidate =>
                candidate.Wins + candidate.Losses >= MinimumGamesPlayed
                && ((candidate.Player1Id == crewUserContext.UserId
                        && activePartnerUserIds.Contains(candidate.Player2Id))
                    || (candidate.Player2Id == crewUserContext.UserId
                        && activePartnerUserIds.Contains(candidate.Player1Id)))
            )
            .Select(candidate => new BestPartnerStatisticsTotals
            {
                PartnerUserId = candidate.Player1Id == crewUserContext.UserId
                    ? candidate.Player2Id
                    : candidate.Player1Id,
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
            .ThenBy(candidate => candidate.PartnerUserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (statistics is null)
        {
            return TypedResults.Ok(new GetBestPartnerHttpResponse
            {
                BestPartner = null,
            });
        }

        var partner = activePartnersByUserId[statistics.PartnerUserId];

        return TypedResults.Ok(new GetBestPartnerHttpResponse
        {
            BestPartner = new BestPartnerHttpResponse
            {
                UserId = partner.WorkOsUserId,
                DisplayName = partner.DisplayName,
                ProfilePictureUrl = partner.ProfilePictureUrl,
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
}

internal sealed record BestPartnerStatisticsTotals
{
    public required string PartnerUserId { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }
}
