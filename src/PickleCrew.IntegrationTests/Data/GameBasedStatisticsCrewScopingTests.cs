using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using Xunit;

namespace PickleCrew.IntegrationTests.Data;

[Collection(nameof(AppHostCollection))]
public sealed class GameBasedStatisticsCrewScopingTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Queries_WhenContextsHaveDifferentCrews_ShouldIsolateGameBasedStatistics()
    {
        await using var database = fixture.CreateDbContext();
        var firstCrew = CreateCrew("First Game Statistics Crew");
        var secondCrew = CreateCrew("Second Game Statistics Crew");

        database.AddRange(
            CreateProvisioningAttempt(firstCrew),
            CreateProvisioningAttempt(secondCrew),
            firstCrew,
            secondCrew
        );
        await database.SaveChangesAsync();

        var firstGame = CreateGame(firstCrew);
        var secondGame = CreateGame(secondCrew);
        database.AddRange(firstGame, secondGame);
        await database.SaveChangesAsync();

        database.AddRange(
            CreateMemberGameStatistics(firstGame),
            CreateMemberGameStatistics(secondGame),
            CreateMatchupGameStatistics(firstGame),
            CreateMatchupGameStatistics(secondGame),
            CreatePairingGameStatistics(firstGame),
            CreatePairingGameStatistics(secondGame),
            CreateWinStreak(firstCrew),
            CreateWinStreak(secondCrew)
        );
        await database.SaveChangesAsync();

        await using var firstCrewDatabase = CreateCrewScopedDbContext(firstCrew);
        await using var secondCrewDatabase = CreateCrewScopedDbContext(secondCrew);

        await AssertScopedToCrew(firstCrewDatabase, firstCrew);
        await AssertScopedToCrew(secondCrewDatabase, secondCrew);
    }

    private static async Task AssertScopedToCrew(
        CrewScopedDbContext database,
        Crew expectedCrew)
    {
        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .SingleAsync();
        var matchupStatistics = await database.MatchupStatistics
            .AsNoTracking()
            .SingleAsync();
        var pairingStatistics = await database.DoublesPairingStatistics
            .AsNoTracking()
            .SingleAsync();
        var winStreak = await database.PlayerWinStreaks
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(expectedCrew.Id, memberStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, memberStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, matchupStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, matchupStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, pairingStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, pairingStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, winStreak.CrewId);
        Assert.Equal(expectedCrew.Uuid, winStreak.CrewUuid);
    }

    private static Crew CreateCrew(string name)
    {
        return new Crew
        {
            Name = $"{name} {Guid.NewGuid():N}",
            WorkOsOrganizationId = $"org_{Guid.NewGuid():N}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static CrewProvisioningAttempt CreateProvisioningAttempt(Crew crew)
    {
        return new CrewProvisioningAttempt
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
    }

    private static Game CreateGame(Crew crew)
    {
        return new Game
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            Format = GameFormat.Doubles,
            RecorderUserId = $"user_{Guid.NewGuid():N}",
            PlayedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static PlayerStatistics CreateMemberGameStatistics(Game game)
    {
        return new PlayerStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            UserId = $"user_{Guid.NewGuid():N}",
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

    private static MatchupStatistics CreateMatchupGameStatistics(Game game)
    {
        return new MatchupStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            UserId = $"user_{Guid.NewGuid():N}",
            OpponentUserId = $"user_{Guid.NewGuid():N}",
            PlayedAtUtc = game.PlayedAtUtc,
            Wins = 1,
            Losses = 0,
            PointsScored = 11,
            PointsConceded = 4,
            PointDifferential = 7,
        };
    }

    private static DoublesPairingStatistics CreatePairingGameStatistics(Game game)
    {
        var key = CrewDoublesPairingKey.Create(
            $"user_{Guid.NewGuid():N}",
            $"user_{Guid.NewGuid():N}"
        );

        return new DoublesPairingStatistics
        {
            GameId = game.Id,
            GameUuid = game.Uuid,
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            Player1Id = key.Player1Id,
            Player2Id = key.Player2Id,
            PlayedAtUtc = game.PlayedAtUtc,
            Wins = 1,
            Losses = 0,
            PointsScored = 11,
            PointsConceded = 4,
            PointDifferential = 7,
        };
    }

    private static PlayerWinStreak CreateWinStreak(Crew crew)
    {
        return new PlayerWinStreak
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            UserId = $"user_{Guid.NewGuid():N}",
            CurrentWinStreak = 0,
            CurrentWinStreakStartedAtUtc = null,
            LongestWinStreak = 0,
            LongestWinStreakStartedAtUtc = null,
            LongestWinStreakEndedAtUtc = null,
        };
    }

    private CrewScopedDbContext CreateCrewScopedDbContext(Crew crew)
    {
        var options = new DbContextOptionsBuilder<CrewScopedDbContext>()
            .UseNpgsql(fixture.DatabaseConnectionString)
            .Options;
        var crewUserContext = new CrewUserContext
        {
            CrewId = crew.Id,
        };

        return new CrewScopedDbContext(options, crewUserContext);
    }
}
