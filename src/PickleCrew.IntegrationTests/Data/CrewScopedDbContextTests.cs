using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Services;
using Xunit;

namespace PickleCrew.IntegrationTests.Data;

[Collection(nameof(AppHostCollection))]
public sealed class CrewScopedDbContextTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Queries_WhenContextsHaveDifferentCrews_ShouldIsolateEachCrew()
    {
        await using var database = fixture.CreateDbContext();
        var firstCrew = CreateCrew("First Crew");
        var secondCrew = CreateCrew("Second Crew");

        database.AddRange(
            CreateProvisioningAttempt(firstCrew),
            CreateProvisioningAttempt(secondCrew),
            firstCrew,
            secondCrew
        );
        await database.SaveChangesAsync();

        database.AddRange(
            CreateGame(firstCrew),
            CreateGame(secondCrew),
            CreateMemberStatistics(firstCrew),
            CreateMemberStatistics(secondCrew),
            CreatePairingStatistics(firstCrew),
            CreatePairingStatistics(secondCrew),
            CreateMatchupStatistics(firstCrew),
            CreateMatchupStatistics(secondCrew)
        );
        await database.SaveChangesAsync();

        await using var firstCrewDatabase = CreateCrewScopedDbContext(firstCrew);
        await using var secondCrewDatabase = CreateCrewScopedDbContext(secondCrew);

        await AssertScopedToCrew(firstCrewDatabase, firstCrew);
        await AssertScopedToCrew(secondCrewDatabase, secondCrew);
    }

    [Fact]
    public async Task CrewMemberStatistics_WhenBaseStatisticsAreSaved_ShouldPersistQueryableCalculatedValues()
    {
        await using var database = fixture.CreateDbContext();
        var crew = CreateCrew("Calculated Statistics Crew");

        database.AddRange(
            CreateProvisioningAttempt(crew),
            crew
        );
        await database.SaveChangesAsync();

        var statistics = new CrewMemberStatistics
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            UserId = $"user_{Guid.NewGuid():N}",
            Wins = 2,
            Losses = 1,
            PointsScored = 35,
            PointsConceded = 29,
        };
        database.CrewMemberStatistics.Add(statistics);
        await database.SaveChangesAsync();
        database.ChangeTracker.Clear();

        var persistedStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewScore > 200)
            .OrderByDescending(candidate => candidate.WinPercentage)
            .SingleAsync(candidate => candidate.UserId == statistics.UserId);

        Assert.Equal(3, persistedStatistics.GamesPlayed);
        Assert.Equal(2m / 3m, persistedStatistics.WinPercentage);
        Assert.Equal(6, persistedStatistics.PointDifferential);
        Assert.Equal(206, persistedStatistics.CrewScore);
    }

    private static async Task AssertScopedToCrew(
        CrewScopedDbContext database,
        Crew expectedCrew)
    {
        var memberStatistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .SingleAsync();
        var pairingStatistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .SingleAsync();
        var matchupStatistics = await database.CrewMatchupStatistics
            .AsNoTracking()
            .SingleAsync();
        var game = await database.Games.AsNoTracking().SingleAsync();

        Assert.Equal(expectedCrew.Id, memberStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, memberStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, pairingStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, pairingStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, matchupStatistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, matchupStatistics.CrewUuid);
        Assert.Equal(expectedCrew.Id, game.CrewId);
        Assert.Equal(expectedCrew.Uuid, game.CrewUuid);
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
            Format = GameFormat.Singles,
            RecorderUserId = $"user_{Guid.NewGuid():N}",
            PlayedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    private static CrewMemberStatistics CreateMemberStatistics(Crew crew)
    {
        return new CrewMemberStatistics
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            UserId = $"user_{Guid.NewGuid():N}",
        };
    }

    private static CrewDoublesPairingStatistics CreatePairingStatistics(Crew crew)
    {
        var key = CrewDoublesPairingKey.Create(
            $"user_{Guid.NewGuid():N}",
            $"user_{Guid.NewGuid():N}"
        );

        return new CrewDoublesPairingStatistics
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            Player1Id = key.Player1Id,
            Player2Id = key.Player2Id,
        };
    }

    private static CrewMatchupStatistics CreateMatchupStatistics(Crew crew)
    {
        return new CrewMatchupStatistics
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            UserId = $"user_{Guid.NewGuid():N}",
            OpponentUserId = $"user_{Guid.NewGuid():N}",
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
