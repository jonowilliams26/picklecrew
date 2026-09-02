using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.Statistics.Services;

internal sealed class StatisticsRebuildHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<StatisticsRebuildHostedService> logger)
    : IHostedService
{
    public const string EnabledConfigurationKey = "RebuildStatistics";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var rebuildService = scope.ServiceProvider
            .GetRequiredService<StatisticsRebuildService>();
        var result = await rebuildService.RebuildAsync(cancellationToken);

        logger.LogInformation(
            "Recalculated {GameCount} games into {LegacyMemberCount} legacy member, {LegacyPairingCount} legacy pairing, {LegacyMatchupCount} legacy matchup, {GameMemberCount} game-based member, {GamePairingCount} game-based pairing, {GameMatchupCount} game-based matchup, and {WinStreakCount} win-streak records.",
            result.GamesRecalculated,
            result.CrewMemberStatisticsCreated,
            result.DoublesPairingStatisticsCreated,
            result.MatchupStatisticsCreated,
            result.CrewMemberGameStatisticsCreated,
            result.DoublesPairingGameStatisticsCreated,
            result.MatchupGameStatisticsCreated,
            result.CrewMemberWinStreaksCreated
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
        var executionStrategy = database.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteInTransactionAsync(
            RebuildInTransactionAsync,
            static _ => Task.FromResult(false),
            cancellationToken
        );
    }

    private async Task<StatisticsRebuildResult> RebuildInTransactionAsync(
        CancellationToken cancellationToken)
    {
        database.ChangeTracker.Clear();

        await database.Database.ExecuteSqlRawAsync(
            """
            LOCK TABLE
                "Games",
                "CrewMemberStatistics",
                "CrewDoublesPairingStatistics",
                "CrewMatchupStatistics",
                "CrewMemberGameStatistics",
                "CrewDoublesPairingGameStatistics",
                "CrewMatchupGameStatistics",
                "CrewMemberWinStreaks"
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

        await database.CrewMatchupGameStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.CrewDoublesPairingGameStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.CrewMemberGameStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.CrewMemberWinStreaks
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
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
        var statisticsServiceV2 = new GameStatisticsServiceV2(database);

        foreach (var game in games)
        {
            crewUserContext.CrewId = game.CrewId;
            crewUserContext.CrewUuid = game.CrewUuid;
            database.Attach(game);

            await statisticsService.CalculateStatisticsAsync(
                game,
                cancellationToken
            );
            await statisticsServiceV2.CalculateStatisticsAsync(
                game,
                cancellationToken
            );
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
            CrewMemberGameStatisticsCreated = await database.CrewMemberGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            DoublesPairingGameStatisticsCreated = await database.CrewDoublesPairingGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            MatchupGameStatisticsCreated = await database.CrewMatchupGameStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            CrewMemberWinStreaksCreated = await database.CrewMemberWinStreaks
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
        };

        return result;
    }
}

internal sealed record StatisticsRebuildResult
{
    public required int GamesRecalculated { get; init; }

    public required int CrewMemberStatisticsCreated { get; init; }

    public required int DoublesPairingStatisticsCreated { get; init; }

    public required int MatchupStatisticsCreated { get; init; }

    public required int CrewMemberGameStatisticsCreated { get; init; }

    public required int DoublesPairingGameStatisticsCreated { get; init; }

    public required int MatchupGameStatisticsCreated { get; init; }

    public required int CrewMemberWinStreaksCreated { get; init; }
}
