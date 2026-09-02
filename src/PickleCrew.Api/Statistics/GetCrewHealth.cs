using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetCrewHealthHttpResponse
{
    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required CrewHealthDayHttpResponse[] Days { get; init; }
}

public sealed record CrewHealthDayHttpResponse
{
    public required DateOnly Date { get; init; }

    public required int GameCount { get; init; }
}

internal sealed class GetCrewHealthEndpoint : IEndpoint
{
    private const int CalendarDayCount = 365;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/crew-health", HandleAsync)
            .WithName("GetCrewHealth")
            .WithSummary("Gets daily Game activity for the active Crew over the last year.");
    }

    private static async Task<Ok<GetCrewHealthHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        HybridCache cache,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var endDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var response = await cache.GetOrCreateAsync(
            new CrewHealthCacheOption(
                crewUserContext.CrewUuid,
                endDate
            ),
            factoryCancellationToken => GetResponseAsync(
                endDate,
                database,
                factoryCancellationToken
            ),
            cancellationToken
        );

        return TypedResults.Ok(response);
    }

    private static async ValueTask<GetCrewHealthHttpResponse> GetResponseAsync(
        DateOnly endDate,
        CrewScopedDbContext database,
        CancellationToken cancellationToken)
    {
        var startDate = endDate.AddDays(-(CalendarDayCount - 1));
        var startAtUtc = new DateTimeOffset(
            startDate.ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );
        var endAtUtc = new DateTimeOffset(
            endDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
            TimeSpan.Zero
        );
        var days = await database.Games
            .Where(game =>
                game.PlayedAtUtc >= startAtUtc
                && game.PlayedAtUtc < endAtUtc
            )
            .GroupBy(game => game.PlayedAtUtc.Date)
            .OrderBy(group => group.Key)
            .Select(group => new CrewHealthDayHttpResponse
            {
                Date = DateOnly.FromDateTime(group.Key),
                GameCount = group.Count(),
            })
            .ToArrayAsync(cancellationToken);

        return new GetCrewHealthHttpResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            Days = days,
        };
    }
}
