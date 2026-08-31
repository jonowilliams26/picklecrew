using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetNemesisHttpResponse
{
    public required NemesisHttpResponse? Nemesis { get; init; }
}

public sealed record NemesisHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public required string? ProfilePictureUrl { get; init; }

    public required long Losses { get; init; }
}

internal sealed class GetNemesisEndpoint : IEndpoint
{
    private const int MinimumGamesPlayed = 3;

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/nemesis", HandleAsync)
            .WithName("GetNemesis")
            .WithSummary("Gets the signed-in user's Nemesis for the active Crew.");
    }

    private static async Task<Ok<GetNemesisHttpResponse>> HandleAsync(
        CrewUserContext crewUserContext,
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeMemberIds = activeMembersByUserId.Keys.ToArray();
        var matchup = await database.CrewMatchupStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.UserId == crewUserContext.UserId
                && statistics.Losses > 0
                && statistics.Wins + statistics.Losses >= MinimumGamesPlayed
                && activeMemberIds.Contains(statistics.OpponentUserId)
            )
            .OrderByDescending(statistics => statistics.Losses)
            .ThenBy(statistics => statistics.OpponentUserId)
            .Select(statistics => new
            {
                statistics.OpponentUserId,
                statistics.Losses,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (matchup is null)
        {
            return TypedResults.Ok(new GetNemesisHttpResponse
            {
                Nemesis = null,
            });
        }

        var opponent = activeMembersByUserId[matchup.OpponentUserId];

        return TypedResults.Ok(new GetNemesisHttpResponse
        {
            Nemesis = new NemesisHttpResponse
            {
                UserId = opponent.WorkOsUserId,
                DisplayName = opponent.DisplayName,
                ProfilePictureUrl = opponent.ProfilePictureUrl,
                Losses = matchup.Losses,
            },
        });
    }
}
