using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class CrewStandingsService(
    CrewScopedDbContext database,
    ICrewService crewService,
    CrewUserContext crewUserContext,
    HybridCache cache)
{
    public async Task<CrewStanding> GetMemberStandingAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var standings = await GetStandingsAsync(cancellationToken);
        var standing = standings.SingleOrDefault(candidate =>
            string.Equals(candidate.UserId, userId, StringComparison.Ordinal)
        );

        if (standing is not null)
        {
            return standing;
        }

        await cache.RemoveByTagAsync(
            new CrewStatisticsCacheTag(crewUserContext.CrewUuid),
            cancellationToken
        );
        standings = await GetStandingsAsync(cancellationToken);

        return standings.Single(candidate =>
            string.Equals(candidate.UserId, userId, StringComparison.Ordinal)
        );
    }

    public async Task<CrewStanding[]> GetStandingsAsync(
        CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            new CrewStatisticsCacheOption(
                crewUserContext.CrewUuid,
                CrewStatisticsCacheResource.CrewStandings
            ),
            GetUncachedStandingsAsync,
            cancellationToken
        );
    }

    private async ValueTask<CrewStanding[]> GetUncachedStandingsAsync(
        CancellationToken cancellationToken)
    {
        var activeMembersByUserId = await crewService.GetActiveMembersByIdAsync(
            cancellationToken
        );

        var activeMemberUserIds = activeMembersByUserId.Keys.ToArray();

        var statisticsByUserId = await database.PlayerStatistics
            .Where(statistics => activeMemberUserIds.Contains(statistics.UserId))
            .GroupBy(statistics => statistics.UserId)
            .Select(group => new CrewMemberStatisticsTotals
            {
                UserId = group.Key,
                Wins = group.Sum(statistics => statistics.Wins),
                Losses = group.Sum(statistics => statistics.Losses),
                GamesPlayed = group.Sum(statistics => statistics.Wins + statistics.Losses),
                WinPercentage = group.Sum(statistics => statistics.Wins + statistics.Losses) == 0
                    ? 0
                    : (decimal)group.Sum(statistics => statistics.Wins) / group.Sum(statistics => statistics.Wins + statistics.Losses),
                ThatsAPaddlin = group.Sum(statistics => statistics.ThatsAPaddlin),
                PointsScored = group.Sum(statistics => statistics.PointsScored),
                PointsConceded = group.Sum(statistics => statistics.PointsConceded),
                PointDifferential = group.Sum(statistics => statistics.PointDifferential),
                CrewScore = group.Sum(statistics => statistics.CrewScore),
            })
            .ToDictionaryAsync(statistics => statistics.UserId, cancellationToken);

        var winStreaksByUserId = await database.PlayerWinStreaks
            .Where(winStreak => activeMemberUserIds.Contains(winStreak.UserId))
            .Select(winStreak => new PlayerWinStreakTotals
            {
                UserId = winStreak.UserId,
                CurrentWinStreak = winStreak.CurrentWinStreak,
                CurrentWinStreakStartedAtUtc = winStreak.CurrentWinStreakStartedAtUtc,
                LongestWinStreak = winStreak.LongestWinStreak,
                LongestWinStreakStartedAtUtc = winStreak.LongestWinStreakStartedAtUtc,
                LongestWinStreakEndedAtUtc = winStreak.LongestWinStreakEndedAtUtc,
            })
            .ToDictionaryAsync(winStreak => winStreak.UserId, cancellationToken);


        // Backfill any missing statistics or win streaks with default values
        var statistics = activeMembersByUserId.Keys
            .Select(userId => statisticsByUserId.GetValueOrDefault(userId) ?? new CrewMemberStatisticsTotals { UserId = userId })
            .ToArray();

        var rankedStatistics = CrewRankingCalculator.Rank(statistics);

        return rankedStatistics
            .Select(statistics =>
            {
                var member = activeMembersByUserId[statistics.UserId];
                var winStreak = winStreaksByUserId.GetValueOrDefault(statistics.UserId);

                return new CrewStanding
                {
                    UserId = member.WorkOsUserId,
                    DisplayName = member.DisplayName,
                    ProfilePictureUrl = member.ProfilePictureUrl,
                    Rank = statistics.Rank,
                    Wins = statistics.Wins,
                    Losses = statistics.Losses,
                    GamesPlayed = statistics.GamesPlayed,
                    ThatsAPaddlin = statistics.ThatsAPaddlin,
                    CurrentWinStreak = winStreak?.CurrentWinStreak ?? 0,
                    CurrentWinStreakStartedAtUtc = winStreak?.CurrentWinStreakStartedAtUtc,
                    LongestWinStreak = winStreak?.LongestWinStreak ?? 0,
                    LongestWinStreakStartedAtUtc = winStreak?.LongestWinStreakStartedAtUtc,
                    LongestWinStreakEndedAtUtc = winStreak?.LongestWinStreakEndedAtUtc,
                    WinPercentage = statistics.WinPercentage,
                    PointsScored = statistics.PointsScored,
                    PointsConceded = statistics.PointsConceded,
                    PointDifferential = statistics.PointDifferential,
                    AverageMargin = statistics.GamesPlayed == 0
                        ? 0
                        : (decimal)statistics.PointDifferential / statistics.GamesPlayed,
                    CrewScore = statistics.CrewScore,
                    ActiveCrewMemberCount = activeMembersByUserId.Count,
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
    public static CrewMemberStatisticsTotals[] Rank(CrewMemberStatisticsTotals[] statistics)
    {
        var rankedStatistics = statistics
            .OrderByDescending(memberStatistics => memberStatistics.CrewScore)
            .ThenBy(memberStatistics => memberStatistics.UserId, StringComparer.Ordinal)
            .ToArray();

        var previousCrewScore = default(int?);
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

    public required int Wins { get; init; }

    public required int Losses { get; init; }

    public required int GamesPlayed { get; init; }

    public required int ThatsAPaddlin { get; init; }

    public required int CurrentWinStreak { get; init; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; init; }

    public required int LongestWinStreak { get; init; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }

    public required decimal WinPercentage { get; init; }

    public required int PointsScored { get; init; }

    public required int PointsConceded { get; init; }

    public required int PointDifferential { get; init; }

    public required decimal AverageMargin { get; init; }

    public required int CrewScore { get; init; }

    public required int ActiveCrewMemberCount { get; init; }
}

internal sealed record PlayerWinStreakTotals
{
    public required string UserId { get; init; }

    public required int CurrentWinStreak { get; init; }

    public DateTimeOffset? CurrentWinStreakStartedAtUtc { get; init; }

    public required int LongestWinStreak { get; init; }

    public DateTimeOffset? LongestWinStreakStartedAtUtc { get; init; }

    public DateTimeOffset? LongestWinStreakEndedAtUtc { get; init; }
}

internal sealed record CrewMemberStatisticsTotals
{
    public required string UserId { get; init; }

    public int Rank { get; init; }

    public int Wins { get; init; }

    public int Losses { get; init; }

    public int GamesPlayed { get; init; }

    public decimal WinPercentage { get; init; }

    public int ThatsAPaddlin { get; init; }

    public int PointsScored { get; init; }

    public int PointsConceded { get; init; }

    public int PointDifferential { get; init; }

    public int CrewScore { get; init; }
}