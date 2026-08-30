using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Games.RecordGame;
using PickleCrew.Api.Statistics.Services;
using PickleCrew.Api.Services;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class RecordGameMatchupTests(AppHostFixture fixture)
{
    [Fact]
    public async Task RecordGame_WhenMembersOppose_ShouldCreateAndAccumulateDirectionalMatchups()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var firstPlayerId = context.Administrator.UserId;
        var secondPlayerId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var firstResponse = await apiClient.PostAsJsonAsync(
            "/games",
            CreateSinglesRequest(firstPlayerId, secondPlayerId, 11, 7),
            TestJsonSerializerOptions.Default
        );
        firstResponse.EnsureSuccessStatusCode();

        await using (var database = fixture.CreateDbContext())
        {
            var matchups = await GetMatchups(database, context.Uuid);

            Assert.Equal(2, matchups.Length);
            AssertMatchup(
                GetMatchup(matchups, firstPlayerId, secondPlayerId),
                context.Uuid,
                wins: 1,
                losses: 0,
                pointsScored: 11,
                pointsConceded: 7,
                gamesPlayed: 1,
                winPercentage: 1m,
                pointDifferential: 4
            );
            AssertMatchup(
                GetMatchup(matchups, secondPlayerId, firstPlayerId),
                context.Uuid,
                wins: 0,
                losses: 1,
                pointsScored: 7,
                pointsConceded: 11,
                gamesPlayed: 1,
                winPercentage: 0m,
                pointDifferential: -4
            );
        }

        using var secondResponse = await apiClient.PostAsJsonAsync(
            "/games",
            CreateSinglesRequest(firstPlayerId, secondPlayerId, 10, 12),
            TestJsonSerializerOptions.Default
        );
        secondResponse.EnsureSuccessStatusCode();

        await using (var database = fixture.CreateDbContext())
        {
            var matchups = await GetMatchups(database, context.Uuid);

            Assert.Equal(2, matchups.Length);
            AssertMatchup(
                GetMatchup(matchups, firstPlayerId, secondPlayerId),
                context.Uuid,
                wins: 1,
                losses: 1,
                pointsScored: 21,
                pointsConceded: 19,
                gamesPlayed: 2,
                winPercentage: 0.5m,
                pointDifferential: 2
            );
            AssertMatchup(
                GetMatchup(matchups, secondPlayerId, firstPlayerId),
                context.Uuid,
                wins: 1,
                losses: 1,
                pointsScored: 19,
                pointsConceded: 21,
                gamesPlayed: 2,
                winPercentage: 0.5m,
                pointDifferential: -2
            );
        }
    }

    [Fact]
    public async Task RecordGame_WhenFourMembersPlayDoubles_ShouldCreateEveryOpposingDirectionalMatchup()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var winningPlayerIds = new[]
        {
            context.Administrator.UserId,
            context.Members[0].UserId,
        };
        var losingPlayerIds = new[]
        {
            context.Members[1].UserId,
            context.Members[2].UserId,
        };
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(winningPlayerIds[0]),
                    CreateMemberParticipant(winningPlayerIds[1])
                ),
                CreateSide(
                    7,
                    CreateMemberParticipant(losingPlayerIds[0]),
                    CreateMemberParticipant(losingPlayerIds[1])
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );
        response.EnsureSuccessStatusCode();

        await using var database = fixture.CreateDbContext();
        var matchups = await GetMatchups(database, context.Uuid);

        Assert.Equal(8, matchups.Length);

        foreach (var winnerId in winningPlayerIds)
        {
            foreach (var loserId in losingPlayerIds)
            {
                AssertMatchup(
                    GetMatchup(matchups, winnerId, loserId),
                    context.Uuid,
                    wins: 1,
                    losses: 0,
                    pointsScored: 11,
                    pointsConceded: 7,
                    gamesPlayed: 1,
                    winPercentage: 1m,
                    pointDifferential: 4
                );
                AssertMatchup(
                    GetMatchup(matchups, loserId, winnerId),
                    context.Uuid,
                    wins: 0,
                    losses: 1,
                    pointsScored: 7,
                    pointsConceded: 11,
                    gamesPlayed: 1,
                    winPercentage: 0m,
                    pointDifferential: -4
                );
            }
        }

        Assert.DoesNotContain(matchups, matchup =>
            matchup.UserId == winningPlayerIds[0]
            && matchup.OpponentUserId == winningPlayerIds[1]
        );
        Assert.DoesNotContain(matchups, matchup =>
            matchup.UserId == losingPlayerIds[0]
            && matchup.OpponentUserId == losingPlayerIds[1]
        );
    }

    [Fact]
    public async Task RecordGame_WhenDoublesIncludesGuests_ShouldCreateOnlyEligibleMemberMatchups()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var winnerId = context.Administrator.UserId;
        var loserId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    13,
                    CreateMemberParticipant(winnerId),
                    CreateGuestParticipant("Winning Guest")
                ),
                CreateSide(
                    11,
                    CreateMemberParticipant(loserId),
                    CreateGuestParticipant("Losing Guest")
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );
        response.EnsureSuccessStatusCode();

        await using var database = fixture.CreateDbContext();
        var matchups = await GetMatchups(database, context.Uuid);

        Assert.Equal(2, matchups.Length);
        AssertMatchup(
            GetMatchup(matchups, winnerId, loserId),
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 13,
            pointsConceded: 11,
            gamesPlayed: 1,
            winPercentage: 1m,
            pointDifferential: 2
        );
        AssertMatchup(
            GetMatchup(matchups, loserId, winnerId),
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 11,
            pointsConceded: 13,
            gamesPlayed: 1,
            winPercentage: 0m,
            pointDifferential: -2
        );
    }

    [Fact]
    public async Task RecordGame_WhenSameMembersPlayInDifferentCrews_ShouldKeepMatchupsSeparate()
    {
        var firstPlayerId = $"user_test_{Guid.NewGuid():N}";
        var secondPlayerId = $"user_test_{Guid.NewGuid():N}";
        var firstCrew = await fixture.CreateCrewBuilder()
            .WithName("First Matchup Crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        var secondCrew = await fixture.CreateCrewBuilder()
            .WithName("Second Matchup Crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        using var firstApiClient = fixture.CreateApiClient(firstCrew.Administrator);
        using var secondApiClient = fixture.CreateApiClient(secondCrew.Administrator);

        using var firstResponse = await firstApiClient.PostAsJsonAsync(
            "/games",
            CreateSinglesRequest(firstPlayerId, secondPlayerId, 11, 7),
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await secondApiClient.PostAsJsonAsync(
            "/games",
            CreateSinglesRequest(firstPlayerId, secondPlayerId, 7, 11),
            TestJsonSerializerOptions.Default
        );
        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        await using var database = fixture.CreateDbContext();
        var firstPlayerMatchups = await database.CrewMatchupStatistics
            .AsNoTracking()
            .Where(matchup =>
                matchup.UserId == firstPlayerId
                && matchup.OpponentUserId == secondPlayerId
            )
            .ToDictionaryAsync(matchup => matchup.CrewUuid);

        Assert.Equal(2, firstPlayerMatchups.Count);
        AssertMatchup(
            firstPlayerMatchups[firstCrew.Uuid],
            firstCrew.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7,
            gamesPlayed: 1,
            winPercentage: 1m,
            pointDifferential: 4
        );
        AssertMatchup(
            firstPlayerMatchups[secondCrew.Uuid],
            secondCrew.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11,
            gamesPlayed: 1,
            winPercentage: 0m,
            pointDifferential: -4
        );
    }

    [Fact]
    public async Task SaveChanges_WhenMatchupHasConcurrencyConflict_ShouldRollBackGameAndOtherStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var winnerId = context.Administrator.UserId;
        var loserId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        using var seedResponse = await apiClient.PostAsJsonAsync(
            "/games",
            CreateSinglesRequest(winnerId, loserId, 11, 7),
            TestJsonSerializerOptions.Default
        );
        seedResponse.EnsureSuccessStatusCode();

        var options = new DbContextOptionsBuilder<CrewScopedDbContext>()
            .UseNpgsql(fixture.DatabaseConnectionString)
            .Options;
        var crewUserContext = new CrewUserContext
        {
            CrewId = context.Id,
            CrewUuid = context.Uuid,
        };
        await using var staleDatabase = new CrewScopedDbContext(
            options,
            crewUserContext
        );
        var staleGame = CreateGame(
            context.Id,
            context.Uuid,
            winnerId,
            loserId,
            11,
            8
        );
        var statisticsService = new GameStatisticsService(staleDatabase);
        var changes = await statisticsService.CalculateStatisticsAsync(
            staleGame,
            CancellationToken.None
        );
        Assert.Equal(2, changes.MatchupStatistics.Length);
        Assert.All(
            changes.MatchupStatistics,
            change => Assert.Equal(
                CalculateGameStatisticsResultOperation.Updated,
                change.Operation
            )
        );
        staleDatabase.Games.Add(staleGame);

        await using (var competingDatabase = fixture.CreateDbContext())
        {
            var competingMatchup = await competingDatabase.CrewMatchupStatistics
                .SingleAsync(matchup =>
                    matchup.CrewId == context.Id
                    && matchup.UserId == winnerId
                    && matchup.OpponentUserId == loserId
                );
            competingMatchup.Losses += 1;
            competingMatchup.PointsScored += 7;
            competingMatchup.PointsConceded += 11;
            await competingDatabase.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await staleDatabase.SaveChangesAsync()
        );

        await using var verificationDatabase = fixture.CreateDbContext();
        Assert.Equal(
            1,
            await verificationDatabase.Games.CountAsync(game =>
                game.CrewId == context.Id
            )
        );
        var memberStatistics = await verificationDatabase.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == context.Id)
            .ToDictionaryAsync(statistics => statistics.UserId);
        Assert.Equal(1, memberStatistics[winnerId].Wins);
        Assert.Equal(0, memberStatistics[winnerId].Losses);
        Assert.Equal(0, memberStatistics[loserId].Wins);
        Assert.Equal(1, memberStatistics[loserId].Losses);

        var matchups = await GetMatchups(verificationDatabase, context.Uuid);
        AssertMatchup(
            GetMatchup(matchups, winnerId, loserId),
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 18,
            pointsConceded: 18,
            gamesPlayed: 2,
            winPercentage: 0.5m,
            pointDifferential: 0
        );
        AssertMatchup(
            GetMatchup(matchups, loserId, winnerId),
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11,
            gamesPlayed: 1,
            winPercentage: 0m,
            pointDifferential: -4
        );
    }

    private static RecordGameHttpRequest CreateSinglesRequest(
        string firstPlayerId,
        string secondPlayerId,
        int firstScore,
        int secondScore)
    {
        return new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides =
            [
                CreateSide(firstScore, CreateMemberParticipant(firstPlayerId)),
                CreateSide(secondScore, CreateMemberParticipant(secondPlayerId)),
            ],
        };
    }

    private static RecordGameSideHttpRequest CreateSide(
        int score,
        params RecordGameParticipantHttpRequest[] participants)
    {
        return new RecordGameSideHttpRequest
        {
            Score = score,
            Participants = participants,
        };
    }

    private static RecordGameParticipantHttpRequest CreateMemberParticipant(
        string userId)
    {
        return new RecordGameParticipantHttpRequest
        {
            UserId = userId,
        };
    }

    private static RecordGameParticipantHttpRequest CreateGuestParticipant(
        string guestName)
    {
        return new RecordGameParticipantHttpRequest
        {
            GuestName = guestName,
        };
    }

    private static Game CreateGame(
        long crewId,
        Guid crewUuid,
        string winnerId,
        string loserId,
        int winnerScore,
        int loserScore)
    {
        return new Game
        {
            CrewId = crewId,
            CrewUuid = crewUuid,
            Format = GameFormat.Singles,
            RecorderUserId = winnerId,
            PlayedAtUtc = DateTimeOffset.UtcNow,
            Sides =
            [
                new GameSide
                {
                    SideNumber = 1,
                    Score = winnerScore,
                    Participants =
                    [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = winnerId,
                        },
                    ],
                },
                new GameSide
                {
                    SideNumber = 2,
                    Score = loserScore,
                    Participants =
                    [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = loserId,
                        },
                    ],
                },
            ],
        };
    }

    private static Task<CrewMatchupStatistics[]> GetMatchups(
        PickleCrewDbContext database,
        Guid crewUuid)
    {
        return database.CrewMatchupStatistics
            .AsNoTracking()
            .Where(matchup => matchup.CrewUuid == crewUuid)
            .ToArrayAsync();
    }

    private static CrewMatchupStatistics GetMatchup(
        CrewMatchupStatistics[] matchups,
        string userId,
        string opponentUserId)
    {
        return Assert.Single(matchups, matchup =>
            matchup.UserId == userId
            && matchup.OpponentUserId == opponentUserId
        );
    }

    private static void AssertMatchup(
        CrewMatchupStatistics matchup,
        Guid crewUuid,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded,
        long gamesPlayed,
        decimal winPercentage,
        long pointDifferential)
    {
        Assert.Equal(crewUuid, matchup.CrewUuid);
        Assert.Equal(wins, matchup.Wins);
        Assert.Equal(losses, matchup.Losses);
        Assert.Equal(pointsScored, matchup.PointsScored);
        Assert.Equal(pointsConceded, matchup.PointsConceded);
        Assert.Equal(gamesPlayed, matchup.GamesPlayed);
        Assert.Equal(winPercentage, matchup.WinPercentage);
        Assert.Equal(pointDifferential, matchup.PointDifferential);
        Assert.NotEqual(0u, matchup.Version);
    }
}
