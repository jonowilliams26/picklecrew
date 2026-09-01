using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetLimpWristSyndromeHttpResponse
{
    public required LimpWristSyndromeHttpResponse? LimpWristSyndrome { get; init; }
}

public sealed record LimpWristSyndromeHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required long Losses { get; init; }

    public required long PointDifferential { get; init; }
}

internal sealed class GetLimpWristSyndromeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/limp-wrist-syndrome", HandleAsync)
            .WithName("GetLimpWristSyndrome")
            .WithSummary("Gets the limp wrist syndrome holder for the active Crew.");
    }

    private static async Task<Ok<GetLimpWristSyndromeHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        IServiceScopeFactory scopeFactory,
        HybridCache cache,
        CancellationToken cancellationToken)
    {
        var response = await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.LimpWristSyndrome
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

    private static async ValueTask<GetLimpWristSyndromeHttpResponse> GetResponseAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();
        var holderStatistics = await database.CrewMemberStatistics
            .Where(statistics =>
                activeMemberUserIds.Contains(statistics.UserId)
                && statistics.Losses > 0
            )
            .OrderByDescending(statistics => statistics.Losses)
            .ThenBy(statistics => statistics.PointDifferential)
            .ThenBy(statistics => statistics.UserId)
            .Select(statistics => new
            {
                statistics.UserId,
                statistics.Losses,
                statistics.PointDifferential,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (holderStatistics is null)
        {
            return new GetLimpWristSyndromeHttpResponse
            {
                LimpWristSyndrome = null,
            };
        }

        var holder = activeMembersByUserId[holderStatistics.UserId];

        return new GetLimpWristSyndromeHttpResponse
        {
            LimpWristSyndrome = new LimpWristSyndromeHttpResponse
            {
                UserId = holder.WorkOsUserId,
                DisplayName = holder.DisplayName,
                ProfilePictureUrl = holder.ProfilePictureUrl,
                Losses = holderStatistics.Losses,
                PointDifferential = holderStatistics.PointDifferential,
            },
        };
    }
}
