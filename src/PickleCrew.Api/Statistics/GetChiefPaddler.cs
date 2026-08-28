using Microsoft.AspNetCore.Http.HttpResults;
using PickleCrew.Api.Statistics.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetChiefPaddlerHttpResponse
{
    public required ChiefPaddlerHttpResponse? ChiefPaddler { get; init; }
}

public sealed record ChiefPaddlerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required long ThatsAPaddlin { get; init; }
}

internal sealed class GetChiefPaddlerEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/chief-paddler", HandleAsync)
            .WithName("GetChiefPaddler")
            .WithSummary("Gets the Chief Paddler for the active Crew.");
    }

    private static async Task<Ok<GetChiefPaddlerHttpResponse>> HandleAsync(
        CrewStandingsService standingsService,
        CancellationToken cancellationToken)
    {
        var standings = await standingsService.GetStandingsAsync(cancellationToken);
        var chiefPaddler = standings
            .Where(standing => standing.ThatsAPaddlin > 0)
            .OrderByDescending(standing => standing.ThatsAPaddlin)
            .ThenBy(standing => standing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(standing => standing.UserId, StringComparer.Ordinal)
            .FirstOrDefault();

        return TypedResults.Ok(new GetChiefPaddlerHttpResponse
        {
            ChiefPaddler = chiefPaddler is null
                ? null
                : new ChiefPaddlerHttpResponse
                {
                    UserId = chiefPaddler.UserId,
                    DisplayName = chiefPaddler.DisplayName,
                    ProfilePictureUrl = chiefPaddler.ProfilePictureUrl,
                    ThatsAPaddlin = chiefPaddler.ThatsAPaddlin,
                },
        });
    }
}
