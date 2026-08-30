using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class StatisticsRebuildHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<StatisticsRebuildHostedService> logger)
    : IHostedService
{
    public const string EnabledConfigurationKey = "REBUILD_STATISTICS";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var rebuildService = scope.ServiceProvider
            .GetRequiredService<StatisticsRebuildService>();
        var result = await rebuildService.RebuildAsync(cancellationToken);

        logger.LogInformation(
            "Recalculated {GameCount} games into {MemberCount} member, {PairingCount} pairing, and {MatchupCount} matchup statistics records.",
            result.GamesRecalculated,
            result.CrewMemberStatisticsCreated,
            result.DoublesPairingStatisticsCreated,
            result.MatchupStatisticsCreated
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class StatisticsRebuildService(
    CrewScopedDbContext database,
    CrewUserContext crewUserContext)
{
    public async Task<StatisticsRebuildResult> RebuildAsync(
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(
            cancellationToken
        );

        await database.Database.ExecuteSqlRawAsync(
            """
            LOCK TABLE
                "Games",
                "CrewMemberStatistics",
                "CrewDoublesPairingStatistics",
                "CrewMatchupStatistics"
            IN ACCESS EXCLUSIVE MODE;
            """,
            cancellationToken
        );

        var games = await database.Games
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(game => game.PlayedAtUtc)
            .ThenBy(game => game.Id)
            .ToArrayAsync(cancellationToken);

        await database.CrewMatchupStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.CrewDoublesPairingStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.CrewMemberStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);

        var statisticsService = new GameStatisticsService(database);

        foreach (var game in games)
        {
            crewUserContext.CrewId = game.CrewId;
            crewUserContext.CrewUuid = game.CrewUuid;

            await statisticsService.CalculateStatisticsAsync(game, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            database.ChangeTracker.Clear();
        }

        var result = new StatisticsRebuildResult
        {
            GamesRecalculated = games.Length,
            CrewMemberStatisticsCreated = await database.CrewMemberStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            DoublesPairingStatisticsCreated = await database.CrewDoublesPairingStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            MatchupStatisticsCreated = await database.CrewMatchupStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
        };

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}

internal sealed record StatisticsRebuildResult
{
    public required int GamesRecalculated { get; init; }

    public required int CrewMemberStatisticsCreated { get; init; }

    public required int DoublesPairingStatisticsCreated { get; init; }

    public required int MatchupStatisticsCreated { get; init; }
}
