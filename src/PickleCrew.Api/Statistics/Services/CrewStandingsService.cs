using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class CrewStandingsService(
    PickleCrewDbContext database,
    IWorkOsService workOs,
    CrewUserContext crewUserContext)
{
    public async Task<CrewStanding> GetMemberStandingAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var standings = await GetStandingsAsync(cancellationToken);

        return standings.Single(candidate =>
            string.Equals(candidate.UserId, userId, StringComparison.Ordinal)
        );
    }

    public async Task<CrewStanding[]> GetStandingsAsync(
        CancellationToken cancellationToken)
    {
        var activeMembers = await workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );

        var statistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewId == crewUserContext.CrewId)
            .Select(candidate => new CrewMemberStatisticsTotals
            {
                UserId = candidate.UserId,
                Wins = candidate.Wins,
                Losses = candidate.Losses,
                PointsScored = candidate.PointsScored,
                PointsConceded = candidate.PointsConceded,
            })
            .ToArrayAsync(cancellationToken);

        return CrewStandingsCalculator.Calculate(activeMembers, statistics);
    }
}

internal static class CrewStandingsCalculator
{
    public static CrewStanding[] Calculate(
        ActiveCrewMember[] activeMembers,
        CrewMemberStatisticsTotals[] statistics)
    {
        var statisticsByUserId = statistics.ToDictionary(
            candidate => candidate.UserId,
            StringComparer.Ordinal
        );

        var standings = activeMembers
            .Select(member => CreateStanding(
                member,
                statisticsByUserId,
                activeMembers.Length
            ))
            .ToArray();

        return Rank(standings);
    }

    private static CrewStanding[] Rank(CrewStanding[] standings)
    {
        var rankedStandings = standings
            .OrderByDescending(standing => standing.CrewScore)
            .ThenBy(standing => standing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(standing => standing.UserId, StringComparer.Ordinal)
            .ToArray();
        var previousCrewScore = default(long?);
        var rank = 0;

        for (var index = 0; index < rankedStandings.Length; index++)
        {
            var standing = rankedStandings[index];

            if (previousCrewScore != standing.CrewScore)
            {
                rank = index + 1;
                previousCrewScore = standing.CrewScore;
            }

            rankedStandings[index] = standing with { Rank = rank };
        }

        return rankedStandings;
    }

    private static CrewStanding CreateStanding(
        ActiveCrewMember member,
        Dictionary<string, CrewMemberStatisticsTotals> statisticsByUserId,
        int activeCrewMemberCount)
    {
        statisticsByUserId.TryGetValue(member.WorkOsUserId, out var statistics);
        var wins = statistics?.Wins ?? 0;
        var losses = statistics?.Losses ?? 0;
        var pointsScored = statistics?.PointsScored ?? 0;
        var pointsConceded = statistics?.PointsConceded ?? 0;
        var gamesPlayed = wins + losses;

        return new CrewStanding
        {
            UserId = member.WorkOsUserId,
            DisplayName = member.DisplayName,
            ProfilePictureUrl = member.ProfilePictureUrl,
            Rank = 0,
            Wins = wins,
            Losses = losses,
            GamesPlayed = gamesPlayed,
            WinPercentage = gamesPlayed == 0
                ? 0
                : (decimal)wins / gamesPlayed,
            PointsScored = pointsScored,
            PointsConceded = pointsConceded,
            PointDifferential = pointsScored - pointsConceded,
            CrewScore = wins * 100,
            ActiveCrewMemberCount = activeCrewMemberCount,
        };
    }
}

internal sealed record CrewStanding
{
    public required string UserId { get; init; }

    public required string DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }

    public required int Rank { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long GamesPlayed { get; init; }

    public required decimal WinPercentage { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public required long PointDifferential { get; init; }

    public required long CrewScore { get; init; }

    public required int ActiveCrewMemberCount { get; init; }
}

internal sealed record CrewMemberStatisticsTotals
{
    public required string UserId { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }
}
