using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
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

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }
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
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new PersonalStatisticsCacheOption(
                crewUserContext.CrewUuid,
                crewUserContext.UserId,
                PersonalStatisticsCacheResource.BestPartner
            ),
            factoryCancellationToken => GetResponseAsync(
                crewUserContext.UserId,
                database,
                crewService,
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetBestPartnerHttpResponse> GetResponseAsync(
        string userId,
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activePartnerUserIds = activeMembersByUserId.Keys
            .Where(candidateUserId => !string.Equals(
                candidateUserId,
                userId,
                StringComparison.Ordinal
            ))
            .ToArray();

        var statistics = await database.CrewDoublesPairingGameStatistics
            .Where(candidate =>
                (candidate.Player1Id == userId
                    && activePartnerUserIds.Contains(candidate.Player2Id))
                || (candidate.Player2Id == userId
                    && activePartnerUserIds.Contains(candidate.Player1Id))
            )
            .GroupBy(candidate => new
            {
                candidate.Player1Id,
                candidate.Player2Id,
            })
            .Select(group => new BestPartnerStatisticsTotals
            {
                PartnerUserId = group.Key.Player1Id == userId
                    ? group.Key.Player2Id
                    : group.Key.Player1Id,
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
            return new GetBestPartnerHttpResponse
            {
                BestPartner = null,
            };
        }

        var partner = activeMembersByUserId[statistics.PartnerUserId];

        return new GetBestPartnerHttpResponse
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
        };
    }
}

internal sealed record BestPartnerStatisticsTotals
{
    public required string PartnerUserId { get; init; }

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }
}
