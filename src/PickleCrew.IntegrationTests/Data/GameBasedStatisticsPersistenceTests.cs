using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using Xunit;

namespace PickleCrew.IntegrationTests.Data;

[Collection(nameof(AppHostCollection))]
public sealed class GameBasedStatisticsPersistenceTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GameBasedStatistics_WhenRecordsAreSaved_ShouldPersistQueryableValues()
    {
        await using var database = fixture.CreateDbContext();
        var crew = await CreateCrewAsync(database, "Game Statistics Persistence Crew");
        var game = await CreateGameAsync(database, crew);
        var pairingKey = CrewDoublesPairingKey.Create("user_b", "user_a");

        database.AddRange(
            CreateMemberGameStatistics(game, "user_a"),
            CreateMatchupGameStatistics(game, "user_a", "user_c"),
            CreatePairingGameStatistics(
                game,
                pairingKey.Player1Id,
                pairingKey.Player2Id
            ),
            new PlayerWinStreak
            {
                CrewId = crew.Id,
                CrewUuid = crew.Uuid,
                UserId = "user_a",
                CurrentWinStreak = 2,
                CurrentWinStreakStartedAtUtc = game.PlayedAtUtc,
                LongestWinStreak = 3,
                LongestWinStreakStartedAtUtc = game.PlayedAtUtc.AddDays(-1),
                LongestWinStreakEndedAtUtc = game.PlayedAtUtc,
            }
        );
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .SingleAsync(statistics =>
                statistics.GameUuid == game.Uuid &&
                statistics.CrewUuid == crew.Uuid
            );
        var matchupStatistics = await database.MatchupStatistics
            .AsNoTracking()
            .SingleAsync(statistics =>
                statistics.GameUuid == game.Uuid &&
                statistics.CrewUuid == crew.Uuid
            );
        var pairingStatistics = await database.DoublesPairingStatistics
            .AsNoTracking()
            .SingleAsync(statistics =>
                statistics.GameUuid == game.Uuid &&
                statistics.CrewUuid == crew.Uuid
            );
        var winStreak = await database.PlayerWinStreaks
            .AsNoTracking()
            .SingleAsync(candidate => candidate.CrewUuid == crew.Uuid);

        Assert.Equal(game.Id, memberStatistics.GameId);
        Assert.Equal(game.PlayedAtUtc, memberStatistics.PlayedAtUtc);
        Assert.Equal(1, memberStatistics.Wins);
        Assert.Equal(0, memberStatistics.Losses);
        Assert.Equal(11, memberStatistics.PointsScored);
        Assert.Equal(4, memberStatistics.PointsConceded);
        Assert.Equal(7, memberStatistics.PointDifferential);
        Assert.Equal(107, memberStatistics.CrewScore);
        Assert.Equal(0, memberStatistics.ThatsAPaddlin);

        Assert.Equal(game.Id, matchupStatistics.GameId);
        Assert.Equal("user_a", matchupStatistics.UserId);
        Assert.Equal("user_c", matchupStatistics.OpponentUserId);
        Assert.Equal(game.PlayedAtUtc, matchupStatistics.PlayedAtUtc);
        Assert.Equal(7, matchupStatistics.PointDifferential);

        Assert.Equal(game.Id, pairingStatistics.GameId);
        Assert.Equal("user_a", pairingStatistics.Player1Id);
        Assert.Equal("user_b", pairingStatistics.Player2Id);
        Assert.Equal(game.PlayedAtUtc, pairingStatistics.PlayedAtUtc);
        Assert.Equal(7, pairingStatistics.PointDifferential);

        Assert.Equal(2, winStreak.CurrentWinStreak);
        Assert.Equal(3, winStreak.LongestWinStreak);
        Assert.Equal(game.PlayedAtUtc, winStreak.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task PlayerStatistics_WhenGameAndUserAreDuplicated_ShouldRejectDuplicate()
    {
        await using var database = fixture.CreateDbContext();
        var crew = await CreateCrewAsync(database, "Duplicate Member Statistics Crew");
        var game = await CreateGameAsync(database, crew);

        database.PlayerStatistics.Add(
            CreateMemberGameStatistics(game, "user_duplicate")
        );
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        database.PlayerStatistics.Add(
            CreateMemberGameStatistics(game, "user_duplicate")
        );

        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await database.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task MatchupStatistics_WhenDirectionalMatchupIsDuplicated_ShouldRejectDuplicate()
    {
        await using var database = fixture.CreateDbContext();
        var crew = await CreateCrewAsync(database, "Duplicate Matchup Statistics Crew");
        var game = await CreateGameAsync(database, crew);

        database.MatchupStatistics.Add(
            CreateMatchupGameStatistics(game, "user_a", "user_b")
        );
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        database.MatchupStatistics.Add(
            CreateMatchupGameStatistics(game, "user_a", "user_b")
        );

        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await database.SaveChangesAsync()
        );
    }

    [Fact]
    public async Task DoublesPairingStatistics_WhenPairingIsDuplicated_ShouldRejectDuplicate()
    {
        await using var database = fixture.CreateDbContext();
        var crew = await CreateCrewAsync(database, "Duplicate Pairing Statistics Crew");
        var game = await CreateGameAsync(database, crew);

        database.DoublesPairingStatistics.Add(
            CreatePairingGameStatistics(game, "user_a", "user_b")
        );
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        database.DoublesPairingStatistics.Add(
            CreatePairingGameStatistics(game, "user_a", "user_b")
        );

        await Assert.ThrowsAsync<DbUpdateException>(
            async () => await database.SaveChangesAsync()
        );
    }

    private static PlayerStatistics CreateMemberGameStatistics(
        Game game,
        string userId)
    {
        return new PlayerStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            UserId = userId,
            PlayedAtUtc = game.PlayedAtUtc,
            Wins = 1,
            Losses = 0,
            PointsScored = 11,
            PointsConceded = 4,
            PointDifferential = 7,
            CrewScore = 107,
            ThatsAPaddlin = 0,
        };
    }

    private static MatchupStatistics CreateMatchupGameStatistics(
        Game game,
        string userId,
        string opponentUserId)
    {
        return new MatchupStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            UserId = userId,
            OpponentUserId = opponentUserId,
            PlayedAtUtc = game.PlayedAtUtc,
            Wins = 1,
            Losses = 0,
            PointsScored = 11,
            PointsConceded = 4,
            PointDifferential = 7,
        };
    }

    private static DoublesPairingStatistics CreatePairingGameStatistics(
        Game game,
        string player1Id,
        string player2Id)
    {
        return new DoublesPairingStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            Player1Id = player1Id,
            Player2Id = player2Id,
            PlayedAtUtc = game.PlayedAtUtc,
            Wins = 1,
            Losses = 0,
            PointsScored = 11,
            PointsConceded = 4,
            PointDifferential = 7,
        };
    }

    private static async Task<Crew> CreateCrewAsync(
        DbContext database,
        string name)
    {
        var crew = new Crew
        {
            Name = $"{name} {Guid.NewGuid():N}",
            WorkOsOrganizationId = $"org_{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        var provisioningAttempt = new CrewProvisioningAttempt
        {
            CrewUuid = crew.Uuid,
            RequestId = Guid.NewGuid(),
            CrewName = crew.Name,
            WorkOsUserId = $"user_{Guid.NewGuid():N}",
            WorkOsOrganizationId = crew.WorkOsOrganizationId,
            WorkOsMembershipId = $"om_{Guid.NewGuid():N}",
            StartedAtUtc = crew.CreatedAtUtc,
            CompletedAtUtc = crew.CreatedAtUtc,
        };

        database.AddRange(provisioningAttempt, crew);
        await database.SaveChangesAsync();

        return crew;
    }

    private static async Task<Game> CreateGameAsync(DbContext database, Crew crew)
    {
        var game = new Game
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            Format = GameFormat.Doubles,
            RecorderUserId = $"user_{Guid.NewGuid():N}",
            PlayedAtUtc = DateTimeOffset.UtcNow,
        };

        database.Add(game);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        return await database.Set<Game>()
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Id == game.Id);
    }
}
