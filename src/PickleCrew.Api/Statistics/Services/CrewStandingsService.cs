using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class CrewStandingsService(
    CrewScopedDbContext database,
    ICrewService crewService)
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
        var activeMembers = await crewService.ListActiveCrewMembersAsync(cancellationToken);

        var activeMembersByUserId = activeMembers.ToDictionary(
            member => member.WorkOsUserId,
            StringComparer.Ordinal
        );

        var persistedStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => activeMembersByUserId.Keys.Contains(statistics.UserId))
            .Select(statistics => new CrewMemberStatisticsTotals
            {
                UserId = statistics.UserId,
                Wins = statistics.Wins,
                Losses = statistics.Losses,
                CurrentWinStreak = statistics.CurrentWinStreak,
                CurrentWinStreakStartedAtUtc = statistics.CurrentWinStreakStartedAtUtc,
                LongestWinStreak = statistics.LongestWinStreak,
                LongestWinStreakStartedAtUtc = statistics.LongestWinStreakStartedAtUtc,
                LongestWinStreakEndedAtUtc = statistics.LongestWinStreakEndedAtUtc,
                PointsScored = statistics.PointsScored,
                PointsConceded = statistics.PointsConceded,
            })
            .ToDictionaryAsync(statistics => statistics.UserId, cancellationToken);

        var statistics = activeMembers
            .Select(member => persistedStatistics.GetValueOrDefault(member.WorkOsUserId) ?? new CrewMemberStatisticsTotals
            {
                UserId = member.WorkOsUserId,
                Wins = 0,
                Losses = 0,
                CurrentWinStreak = 0,
                LongestWinStreak = 0,
                PointsScored = 0,
                PointsConceded = 0,
            })
            .ToArray();

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        return rankedStatistics
            .Select(statistics =>
            {
                var member = activeMembersByUserId[statistics.UserId];
                var gamesPlayed = statistics.Wins + statistics.Losses;

                return new CrewStanding
                {
                    UserId = member.WorkOsUserId,
                    DisplayName = member.DisplayName,
                    ProfilePictureUrl = member.ProfilePictureUrl,
                    Rank = statistics.Rank,
                    Wins = statistics.Wins,
                    Losses = statistics.Losses,
                    GamesPlayed = gamesPlayed,
                    CurrentWinStreak = statistics.CurrentWinStreak,
                    CurrentWinStreakStartedAtUtc = statistics.CurrentWinStreakStartedAtUtc,
                    LongestWinStreak = statistics.LongestWinStreak,
                    LongestWinStreakStartedAtUtc = statistics.LongestWinStreakStartedAtUtc,
                    LongestWinStreakEndedAtUtc = statistics.LongestWinStreakEndedAtUtc,
                    WinPercentage = gamesPlayed == 0
                        ? 0
                        : (decimal)statistics.Wins / gamesPlayed,
                    PointsScored = statistics.PointsScored,
                    PointsConceded = statistics.PointsConceded,
                    PointDifferential = statistics.PointDifferential,
                    CrewScore = statistics.CrewScore,
                    ActiveCrewMemberCount = activeMembers.Length,
                };
            })
            .OrderBy(standing => standing.Rank)
            .ThenBy(standing => standing.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(standing => standing.UserId, StringComparer.Ordinal)
            .ToArray();
    }
}

internal static class CrewRankingCalculator
{
    public static CrewMemberStatisticsTotals[] Rank(
        CrewMemberStatisticsTotals[] statistics)
    {
        var rankedStatistics = statistics
            .OrderByDescending(memberStatistics => memberStatistics.CrewScore)
            .ThenBy(memberStatistics => memberStatistics.UserId, StringComparer.Ordinal)
            .ToArray();

        var previousCrewScore = default(long?);
        var rank = 0;

        for (var index = 0; index < rankedStatistics.Length; index++)
        {
            var memberStatistics = rankedStatistics[index];

            if (previousCrewScore != memberStatistics.CrewScore)
            {
                rank = index + 1;
                previousCrewScore = memberStatistics.CrewScore;
            }

            rankedStatistics[index] = memberStatistics with { Rank = rank };
        }

        return rankedStatistics;
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

    public required long CurrentWinStreak { get; init; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; init; }

    public required long LongestWinStreak { get; init; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }

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

    public int Rank { get; init; }

    public required long Wins { get; init; }

    public required long Losses { get; init; }

    public required long CurrentWinStreak { get; init; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; init; }

    public required long LongestWinStreak { get; init; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }

    public required long PointsScored { get; init; }

    public required long PointsConceded { get; init; }

    public long PointDifferential => PointsScored - PointsConceded;

    public long CrewScore => (Wins * 100) + PointDifferential;
}
