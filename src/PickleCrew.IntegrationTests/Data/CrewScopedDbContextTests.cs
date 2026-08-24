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
            CreateStatistics(firstCrew),
            CreateStatistics(secondCrew)
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
        var statistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .SingleAsync();
        var game = await database.Games.AsNoTracking().SingleAsync();

        Assert.Equal(expectedCrew.Id, statistics.CrewId);
        Assert.Equal(expectedCrew.Uuid, statistics.CrewUuid);
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

    private static CrewMemberStatistics CreateStatistics(Crew crew)
    {
        return new CrewMemberStatistics
        {
            CrewId = crew.Id,
            CrewUuid = crew.Uuid,
            UserId = $"user_{Guid.NewGuid():N}",
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
