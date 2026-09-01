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
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new PersonalStatisticsCacheOption(
                crewUserContext.CrewUuid,
                crewUserContext.UserId,
                PersonalStatisticsCacheResource.BestPartner
            ),
            crewUserContext,
            scopeFactory,
            static (services, factoryCancellationToken) => GetResponseAsync(
                services.GetRequiredService<CrewUserContext>().UserId,
                services.GetRequiredService<CrewScopedDbContext>(),
                services.GetRequiredService<ICrewService>(),
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

        var statistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(candidate =>
                candidate.GamesPlayed >= MinimumGamesPlayed
                && ((candidate.Player1Id == userId
                        && activePartnerUserIds.Contains(candidate.Player2Id))
                    || (candidate.Player2Id == userId
                        && activePartnerUserIds.Contains(candidate.Player1Id)))
            )
            .Select(candidate => new BestPartnerStatisticsTotals
            {
                PartnerUserId = candidate.Player1Id == userId
                    ? candidate.Player2Id
                    : candidate.Player1Id,
                Wins = candidate.Wins,
                Losses = candidate.Losses,
                GamesPlayed = candidate.GamesPlayed,
                WinPercentage = candidate.WinPercentage,
                PointsScored = candidate.PointsScored,
                PointsConceded = candidate.PointsConceded,
                PointDifferential = candidate.PointDifferential,
            })
            .OrderByDescending(candidate => candidate.WinPercentage)
            .ThenByDescending(candidate => candidate.GamesPlayed)
            .ThenByDescending(candidate => candidate.PointDifferential)
            .ThenBy(candidate => candidate.PartnerUserId)
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

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }
}
