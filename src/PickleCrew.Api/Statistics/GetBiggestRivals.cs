using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics;

public sealed record GetBiggestRivalsHttpResponse
{
    public required BiggestRivalryHttpResponse? Rivalry { get; init; }
}

public sealed record BiggestRivalryHttpResponse
{
    public required RivalPlayerHttpResponse[] Players { get; init; }

    public required long GamesPlayed { get; init; }
}

public sealed record RivalPlayerHttpResponse
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required decimal WinPercentage { get; init; }
}

internal sealed class GetBiggestRivalsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/biggest-rivals", HandleAsync)
            .WithName("GetBiggestRivals")
            .WithSummary("Gets the biggest rivals for the active Crew.");
    }

    private static async Task<Ok<GetBiggestRivalsHttpResponse>> HandleAsync(
        CrewScopedDbContext database,
        ICrewService crewService,
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );
        var activeUserIds = activeMembersByUserId.Keys;

        var rivalry = await database.CrewMatchupStatistics
            .AsNoTracking()
            .Where(matchup =>
                activeUserIds.Contains(matchup.UserId)
                && activeUserIds.Contains(matchup.OpponentUserId)
                && matchup.Wins + matchup.Losses >= 3
            )
            .OrderBy(matchup => Math.Abs(
                (decimal)matchup.Wins / (matchup.Wins + matchup.Losses) - 0.5m
            ))
            .ThenByDescending(matchup => matchup.Wins + matchup.Losses)
            .ThenBy(matchup => matchup.UserId)
            .ThenBy(matchup => matchup.OpponentUserId)
            .Select(matchup => new RivalryCandidate
            {
                UserId = matchup.UserId,
                OpponentUserId = matchup.OpponentUserId,
                Wins = matchup.Wins,
                Losses = matchup.Losses,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (rivalry is null)
        {
            return TypedResults.Ok(new GetBiggestRivalsHttpResponse
            {
                Rivalry = null,
            });
        }

        var players = new[]
        {
            CreatePlayer(
                activeMembersByUserId[rivalry.UserId],
                rivalry.Wins,
                rivalry.Losses
            ),
            CreatePlayer(
                activeMembersByUserId[rivalry.OpponentUserId],
                rivalry.Losses,
                rivalry.Wins
            ),
        }
            .OrderBy(player => player.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(player => player.UserId, StringComparer.Ordinal)
            .ToArray();

        return TypedResults.Ok(new GetBiggestRivalsHttpResponse
        {
            Rivalry = new BiggestRivalryHttpResponse
            {
                Players = players,
                GamesPlayed = rivalry.Wins + rivalry.Losses,
            },
        });
    }

    private static RivalPlayerHttpResponse CreatePlayer(
        ActiveCrewMember member,
        long wins,
        long losses)
    {
        return new RivalPlayerHttpResponse
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
            Wins = wins,
            Losses = losses,
            WinPercentage = (decimal)wins / (wins + losses),
        };
    }
}

internal sealed record RivalryCandidate
{
    public required string UserId { get; init; }

    public required string OpponentUserId { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }
}
