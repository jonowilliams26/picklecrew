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
            "Recalculated {GameCount} games into {GameMemberCount} game-based member, {GamePairingCount} game-based pairing, {GameMatchupCount} game-based matchup, and {WinStreakCount} win-streak records.",
            result.GamesRecalculated,
            result.PlayerStatisticsCreated,
            result.DoublesPairingStatisticsCreated,
            result.MatchupStatisticsCreated,
            result.PlayerWinStreaksCreated
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
                "PlayerStatistics",
                "DoublesPairingStatistics",
                "MatchupStatistics",
                "PlayerWinStreaks"
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

        await database.MatchupStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.DoublesPairingStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.PlayerStatistics
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);
        await database.PlayerWinStreaks
            .IgnoreQueryFilters()
            .ExecuteDeleteAsync(cancellationToken);

        var statisticsService = new GameStatisticsService(database);

        foreach (var game in games)
        {
            crewUserContext.CrewId = game.CrewId;
            crewUserContext.CrewUuid = game.CrewUuid;
            database.Attach(game);

            await statisticsService.CalculateStatisticsAsync(
                game,
                cancellationToken
            );
            await database.SaveChangesAsync(cancellationToken);
            database.ChangeTracker.Clear();
        }

        var result = new StatisticsRebuildResult
        {
            GamesRecalculated = games.Length,
            PlayerStatisticsCreated = await database.PlayerStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            DoublesPairingStatisticsCreated = await database.DoublesPairingStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            MatchupStatisticsCreated = await database.MatchupStatistics
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
            PlayerWinStreaksCreated = await database.PlayerWinStreaks
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken),
        };

        return result;
    }
}

internal sealed record StatisticsRebuildResult
{
    public required int GamesRecalculated { get; init; }

    public required int PlayerStatisticsCreated { get; init; }

    public required int DoublesPairingStatisticsCreated { get; init; }

    public required int MatchupStatisticsCreated { get; init; }

    public required int PlayerWinStreaksCreated { get; init; }
}
