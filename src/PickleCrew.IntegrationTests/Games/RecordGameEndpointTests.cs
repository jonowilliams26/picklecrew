using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Games;
using PickleCrew.Api.Games.Services;
using PickleCrew.Api.Services;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class RecordGameEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGameRecordingOptions_WhenRecorderIsActive_ShouldReturnCrewRosterWithoutEmails()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(
                displayName: "Jamie Kitchen",
                profilePictureUrl: "https://example.test/jamie.jpg"
            )
            .BuildAsync();
        var member = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games/recording-options");

        response.EnsureSuccessStatusCode();
        var options = await response.Content.ReadFromJsonAsync<GetGameRecordingOptionsHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(options);
        var returnedMember = Assert.Single(
            options.Members,
            candidate => candidate.UserId == member.UserId
        );
        Assert.Equal("Jamie Kitchen", returnedMember.DisplayName);
        Assert.Equal("https://example.test/jamie.jpg", returnedMember.ProfilePictureUrl);

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(member.EmailAddress, content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordGame_WhenRecorderParticipates_ShouldPersistCompleteResult()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 11, 7),
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(context.Uuid, result.CrewId);
        Assert.Equal(context.Administrator.UserId, result.RecorderUserId);
        Assert.Equal(GameFormat.Singles, result.Format);

        await using var dbContext = fixture.CreateDbContext();
        var crewId = await dbContext.Crews
            .Where(crew => crew.Uuid == context.Uuid)
            .Select(crew => crew.Id)
            .SingleAsync();
        var game = await dbContext.Games
            .AsNoTracking()
            .SingleAsync(candidate => candidate.CrewId == crewId);

        Assert.Equal(result.GameId, game.Uuid);
        Assert.Equal(result.CrewId, game.CrewUuid);
        Assert.Equal(result.RecorderUserId, game.RecorderUserId);
        Assert.Equal(result.Format, game.Format);
        Assert.Collection(
            game.Sides.OrderBy(side => side.SideNumber),
            side => AssertSide(side, 1, 11, context.Administrator.UserId),
            side => AssertSide(side, 2, 7, opponent.UserId)
        );
        AssertResponseMatchesGame(result, game);

        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewId == crewId)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(2, statistics.Count);
        AssertStatistics(
            statistics[context.Administrator.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7
        );
        AssertStatistics(
            statistics[opponent.UserId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
    }

    [Fact]
    public async Task RecordGame_WhenMembersHaveExistingStatistics_ShouldAccumulateContributions()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var firstResponse = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 11, 7),
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 10, 12),
            TestJsonSerializerOptions.Default
        );

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        await using var dbContext = fixture.CreateDbContext();
        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewUuid == context.Uuid)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(2, statistics.Count);
        AssertStatistics(
            statistics[context.Administrator.UserId],
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 21,
            pointsConceded: 19
        );
        AssertStatistics(
            statistics[opponent.UserId],
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 19,
            pointsConceded: 21
        );
        Assert.Equal(
            2,
            statistics[context.Administrator.UserId].Wins
                + statistics[context.Administrator.UserId].Losses
        );
        Assert.Equal(
            2,
            statistics[context.Administrator.UserId].PointsScored
                - statistics[context.Administrator.UserId].PointsConceded
        );
        Assert.Equal(2, await CountGamesAsync(context.Uuid));
    }

    [Fact]
    public async Task RecordGame_WhenDoublesHasFourMembers_ShouldGiveEachMemberTheFullScoreContribution()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var winningTeammate = context.Members[0];
        var firstOpponent = context.Members[1];
        var secondOpponent = context.Members[2];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides = [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(winningTeammate.UserId)
                ),
                CreateSide(
                    7,
                    CreateMemberParticipant(firstOpponent.UserId),
                    CreateMemberParticipant(secondOpponent.UserId)
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();

        await using var dbContext = fixture.CreateDbContext();
        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewUuid == context.Uuid)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(4, statistics.Count);
        AssertStatistics(
            statistics[context.Administrator.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7
        );
        AssertStatistics(
            statistics[winningTeammate.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7
        );
        AssertStatistics(
            statistics[firstOpponent.UserId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
        AssertStatistics(
            statistics[secondOpponent.UserId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
    }

    [Fact]
    public async Task RecordGame_WhenDoublesIncludesMembersAndGuests_ShouldPersistCompleteResult()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var teammate = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides = [
                CreateSide(
                    13,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateGuestParticipant("  Jamie Kitchen  ")
                ),
                CreateSide(
                    11,
                    CreateMemberParticipant(teammate.UserId),
                    CreateGuestParticipant("Taylor Dink")
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(GameFormat.Doubles, result.Format);
        Assert.Equal("Jamie Kitchen", result.Sides[0].Participants[1].GuestName);
        Assert.Equal("Taylor Dink", result.Sides[1].Participants[1].GuestName);

        await using var dbContext = fixture.CreateDbContext();
        var game = await dbContext.Games
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Uuid == result.GameId);
        Assert.Collection(
            game.Sides.OrderBy(side => side.SideNumber),
            side => Assert.Collection(
                side.Participants.OrderBy(participant => participant.SlotNumber),
                participant => AssertMemberParticipant(
                    participant,
                    1,
                    context.Administrator.UserId
                ),
                participant => AssertGuestParticipant(participant, 2, "Jamie Kitchen")
            ),
            side => Assert.Collection(
                side.Participants.OrderBy(participant => participant.SlotNumber),
                participant => AssertMemberParticipant(participant, 1, teammate.UserId),
                participant => AssertGuestParticipant(participant, 2, "Taylor Dink")
            )
        );

        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewUuid == context.Uuid)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(2, statistics.Count);
        AssertStatistics(
            statistics[context.Administrator.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 13,
            pointsConceded: 11
        );
        AssertStatistics(
            statistics[teammate.UserId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 11,
            pointsConceded: 13
        );
    }

    [Fact]
    public async Task RecordGame_WhenSinglesIncludesGuest_ShouldPersistGuest()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides = [
                CreateSide(11, CreateMemberParticipant(context.Administrator.UserId)),
                CreateSide(9, CreateGuestParticipant("Jamie Kitchen")),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal("Jamie Kitchen", result.Sides[1].Participants[0].GuestName);
        Assert.Null(result.Sides[1].Participants[0].UserId);

        await using var dbContext = fixture.CreateDbContext();
        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .SingleAsync(candidate => candidate.CrewUuid == context.Uuid);
        AssertStatistics(
            statistics,
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 9
        );
    }

    [Fact]
    public async Task RecordGame_WhenEveryParticipantIsGuest_ShouldRejectWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides = [
                CreateSide(11, CreateGuestParticipant("Jamie")),
                CreateSide(7, CreateGuestParticipant("Taylor")),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("active member", content, StringComparison.OrdinalIgnoreCase);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenGuestNamesDifferOnlyByCaseAndWhitespace_ShouldRejectWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides = [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateGuestParticipant(" Jamie ")
                ),
                CreateSide(
                    8,
                    CreateGuestParticipant("jamie"),
                    CreateGuestParticipant("Taylor")
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("distinct", content, StringComparison.OrdinalIgnoreCase);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenMemberBelongsToAnotherCrew_ShouldRejectWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var otherCrew = await fixture.CreateCrewBuilder()
            .WithName("Other Crew")
            .WithMember()
            .BuildAsync();
        var otherCrewMember = otherCrew.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(
                context.Administrator.UserId,
                otherCrewMember.UserId,
                11,
                5
            ),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("current Crew", content, StringComparison.OrdinalIgnoreCase);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenIdenticalGameIsRecordedTwice_ShouldPersistBothGames()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = CreateRequest(
            context.Administrator.UserId,
            opponent.UserId,
            11,
            7
        );

        using var firstResponse = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();
        Assert.Equal(2, await CountGamesAsync(context.Uuid));

        await using var dbContext = fixture.CreateDbContext();
        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewUuid == context.Uuid)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(2, statistics.Count);
        AssertStatistics(
            statistics[context.Administrator.UserId],
            context.Uuid,
            wins: 2,
            losses: 0,
            pointsScored: 22,
            pointsConceded: 14
        );
        AssertStatistics(
            statistics[opponent.UserId],
            context.Uuid,
            wins: 0,
            losses: 2,
            pointsScored: 14,
            pointsConceded: 22
        );
    }

    [Fact]
    public async Task RecordGame_WhenActiveRecorderDidNotPlay_ShouldNotGenerateRecorderStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var firstParticipant = context.Members[0];
        var secondParticipant = context.Members[1];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(firstParticipant.UserId, secondParticipant.UserId, 12, 10),
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(context.Administrator.UserId, result.RecorderUserId);
        Assert.DoesNotContain(
            result.Sides.SelectMany(side => side.Participants),
            participant => participant.UserId == context.Administrator.UserId
        );

        await using var dbContext = fixture.CreateDbContext();
        var statistics = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(candidate => candidate.CrewUuid == context.Uuid)
            .ToDictionaryAsync(candidate => candidate.UserId);
        Assert.Equal(2, statistics.Count);
        Assert.DoesNotContain(context.Administrator.UserId, statistics.Keys);
        AssertStatistics(
            statistics[firstParticipant.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 12,
            pointsConceded: 10
        );
        AssertStatistics(
            statistics[secondParticipant.UserId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 10,
            pointsConceded: 12
        );
    }

    [Fact]
    public async Task RecordGame_WhenMemberParticipatesInTwoCrews_ShouldKeepStatisticsSeparate()
    {
        var sharedMemberId = $"user_test_{Guid.NewGuid():N}";
        var firstContext = await fixture.CreateCrewBuilder()
            .WithName("First Crew")
            .WithMember(id: sharedMemberId)
            .BuildAsync();
        var secondContext = await fixture.CreateCrewBuilder()
            .WithName("Second Crew")
            .WithMember(id: sharedMemberId)
            .BuildAsync();
        var sharedMember = firstContext.Members[0];
        using var firstApiClient = fixture.CreateApiClient(firstContext.Administrator);
        using var secondApiClient = fixture.CreateApiClient(secondContext.Administrator);

        using var firstResponse = await firstApiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(
                sharedMember.UserId,
                firstContext.Administrator.UserId,
                11,
                5
            ),
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await secondApiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(
                sharedMember.UserId,
                secondContext.Administrator.UserId,
                7,
                11
            ),
            TestJsonSerializerOptions.Default
        );

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        await using var dbContext = fixture.CreateDbContext();
        var statisticsByCrew = await dbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => statistics.UserId == sharedMember.UserId)
            .ToDictionaryAsync(statistics => statistics.CrewUuid);
        Assert.Equal(2, statisticsByCrew.Count);
        AssertStatistics(
            statisticsByCrew[firstContext.Uuid],
            firstContext.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 5
        );
        AssertStatistics(
            statisticsByCrew[secondContext.Uuid],
            secondContext.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
    }

    [Fact]
    public async Task SaveChanges_WhenStatisticsHaveConcurrencyConflict_ShouldRollBackGameAndStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var winnerUserId = context.Administrator.UserId;
        var loserUserId = $"user_test_{Guid.NewGuid():N}";
        long crewId;

        await using (var seedDbContext = fixture.CreateDbContext())
        {
            crewId = await seedDbContext.Crews
                .Where(crew => crew.Uuid == context.Uuid)
                .Select(crew => crew.Id)
                .SingleAsync();
            seedDbContext.CrewMemberStatistics.AddRange(
                new CrewMemberStatistics
                {
                    CrewId = crewId,
                    CrewUuid = context.Uuid,
                    UserId = winnerUserId,
                    Wins = 2,
                    Losses = 1,
                    PointsScored = 31,
                    PointsConceded = 20,
                },
                new CrewMemberStatistics
                {
                    CrewId = crewId,
                    CrewUuid = context.Uuid,
                    UserId = loserUserId,
                    Wins = 1,
                    Losses = 2,
                    PointsScored = 20,
                    PointsConceded = 31,
                }
            );
            await seedDbContext.SaveChangesAsync();
        }

        var crewUserContext = new CrewUserContext
        {
            CrewId = crewId,
            CrewUuid = context.Uuid,
        };
        var options = new DbContextOptionsBuilder<CrewScopedDbContext>()
            .UseNpgsql(fixture.DatabaseConnectionString)
            .Options;
        await using var staleDbContext = new CrewScopedDbContext(
            options,
            crewUserContext
        );
        var game = new Game
        {
            CrewId = crewId,
            CrewUuid = context.Uuid,
            Format = GameFormat.Singles,
            RecorderUserId = winnerUserId,
            PlayedAtUtc = DateTimeOffset.UtcNow,
            Sides =
            [
                new GameSide
                {
                    SideNumber = 1,
                    Score = 11,
                    Participants =
                    [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = winnerUserId,
                        },
                    ],
                },
                new GameSide
                {
                    SideNumber = 2,
                    Score = 7,
                    Participants =
                    [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = loserUserId,
                        },
                    ],
                },
            ],
        };
        var statisticsService = new GameStatisticsService(staleDbContext);
        var generatedStatistics = await statisticsService.GenerateStatistics(
            game,
            CancellationToken.None
        );
        Assert.Empty(generatedStatistics);
        staleDbContext.CrewMemberStatistics.AddRange(generatedStatistics);
        staleDbContext.Games.Add(game);

        await using (var competingDbContext = fixture.CreateDbContext())
        {
            var competingStatistics = await competingDbContext.CrewMemberStatistics
                .SingleAsync(statistics =>
                    statistics.CrewId == crewId
                    && statistics.UserId == winnerUserId
                );
            competingStatistics.Losses += 1;
            competingStatistics.PointsScored += 7;
            competingStatistics.PointsConceded += 11;
            await competingDbContext.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await staleDbContext.SaveChangesAsync()
        );

        await using var verificationDbContext = fixture.CreateDbContext();
        Assert.Equal(
            0,
            await verificationDbContext.Games.CountAsync(candidate =>
                candidate.CrewId == crewId
            )
        );
        var persistedStatistics = await verificationDbContext.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == crewId)
            .ToDictionaryAsync(statistics => statistics.UserId);
        Assert.Equal(2, persistedStatistics.Count);
        AssertStatistics(
            persistedStatistics[winnerUserId],
            context.Uuid,
            wins: 2,
            losses: 2,
            pointsScored: 38,
            pointsConceded: 31
        );
        AssertStatistics(
            persistedStatistics[loserUserId],
            context.Uuid,
            wins: 1,
            losses: 2,
            pointsScored: 20,
            pointsConceded: 31
        );
    }

    [Theory]
    [InlineData(InvalidGameRequest.MissingSide)]
    [InlineData(InvalidGameRequest.RepeatedMember)]
    [InlineData(InvalidGameRequest.BlankGuestName)]
    [InlineData(InvalidGameRequest.MemberAndGuestIdentity)]
    public async Task RecordGame_WhenRequestIsStructurallyInvalid_ShouldRejectWithoutWritingGameOrStatistics(
        InvalidGameRequest invalidRequest)
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var memberParticipant = CreateMemberParticipant(
            context.Administrator.UserId
        );
        var request = invalidRequest switch
        {
            InvalidGameRequest.MissingSide => new RecordGameHttpRequest
            {
                Format = GameFormat.Singles,
                Sides = [CreateSide(11, memberParticipant)],
            },
            InvalidGameRequest.RepeatedMember => new RecordGameHttpRequest
            {
                Format = GameFormat.Singles,
                Sides = [
                    CreateSide(11, memberParticipant),
                    CreateSide(7, memberParticipant),
                ],
            },
            InvalidGameRequest.BlankGuestName => new RecordGameHttpRequest
            {
                Format = GameFormat.Singles,
                Sides = [
                    CreateSide(11, memberParticipant),
                    CreateSide(7, CreateGuestParticipant("  ")),
                ],
            },
            InvalidGameRequest.MemberAndGuestIdentity => new RecordGameHttpRequest
            {
                Format = GameFormat.Singles,
                Sides = [
                    CreateSide(11, memberParticipant),
                    CreateSide(
                        7,
                        new RecordGameParticipantHttpRequest
                        {
                            UserId = context.Administrator.UserId,
                            GuestName = "Jamie",
                        }
                    ),
                ],
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(invalidRequest),
                invalidRequest,
                null
            ),
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenParticipantIsInactive_ShouldRejectWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(status: FakeMembershipStatus.Inactive)
            .BuildAsync();
        var inactiveParticipant = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(
                context.Administrator.UserId,
                inactiveParticipant.UserId,
                11,
                4
            ),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not an active member", content, StringComparison.OrdinalIgnoreCase);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenRecorderIsInactive_ShouldReturnForbiddenWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember(status: FakeMembershipStatus.Inactive)
            .BuildAsync();
        var activeParticipant = context.Members[0];
        var inactiveRecorder = context.Members[1];
        using var apiClient = fixture.CreateApiClient(inactiveRecorder);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(
                context.Administrator.UserId,
                activeParticipant.UserId,
                11,
                6
            ),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenScoreIsInvalid_ShouldRejectWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 12, 9),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    [Fact]
    public async Task RecordGame_WhenMembershipProviderFails_ShouldReturnProblemDetailsWithoutWritingGameOrStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailOperationAsync(
            scenarioId,
            FakeWorkOsOperation.ListActiveMembers
        );
        using var apiClient = fixture.CreateApiClient(
            context.Administrator,
            scenarioId
        );

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 11, 3),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType
        );
        await AssertNoGameOrStatisticsAsync(context.Uuid);
    }

    private async Task<int> CountGamesAsync(Guid crewUuid)
    {
        await using var dbContext = fixture.CreateDbContext();

        return await dbContext.Games.CountAsync(game => game.Crew.Uuid == crewUuid);
    }

    private async Task AssertNoGameOrStatisticsAsync(Guid crewUuid)
    {
        await using var dbContext = fixture.CreateDbContext();
        var crewId = await dbContext.Crews
            .Where(crew => crew.Uuid == crewUuid)
            .Select(crew => crew.Id)
            .SingleAsync();

        Assert.Equal(0, await dbContext.Games.CountAsync(game => game.CrewId == crewId));
        Assert.Equal(
            0,
            await dbContext.CrewMemberStatistics.CountAsync(statistics =>
                statistics.CrewId == crewId
            )
        );
    }

    private static RecordGameHttpRequest CreateRequest(
        string firstUserId,
        string secondUserId,
        int firstScore,
        int secondScore)
    {
        return new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides = [
                CreateSide(firstUserId, firstScore),
                CreateSide(secondUserId, secondScore),
            ],
        };
    }

    private static RecordGameSideHttpRequest CreateSide(string userId, int score)
    {
        return CreateSide(score, CreateMemberParticipant(userId));
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
        return new RecordGameParticipantHttpRequest { UserId = userId };
    }

    private static RecordGameParticipantHttpRequest CreateGuestParticipant(
        string guestName)
    {
        return new RecordGameParticipantHttpRequest { GuestName = guestName };
    }

    private static void AssertStatistics(
        CrewMemberStatistics statistics,
        Guid crewUuid,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded)
    {
        Assert.Equal(crewUuid, statistics.CrewUuid);
        Assert.Equal(wins, statistics.Wins);
        Assert.Equal(losses, statistics.Losses);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
    }

    private static void AssertMemberParticipant(
        GameParticipant participant,
        int slotNumber,
        string userId)
    {
        Assert.Equal(slotNumber, participant.SlotNumber);
        Assert.Equal(userId, participant.UserId);
        Assert.False(string.IsNullOrWhiteSpace(participant.MemberDisplayName));
        Assert.Null(participant.GuestName);
    }

    private static void AssertGuestParticipant(
        GameParticipant participant,
        int slotNumber,
        string guestName)
    {
        Assert.Equal(slotNumber, participant.SlotNumber);
        Assert.Null(participant.UserId);
        Assert.Null(participant.MemberDisplayName);
        Assert.Equal(guestName, participant.GuestName);
    }

    private static void AssertSide(
        GameSide side,
        int sideNumber,
        int score,
        string userId)
    {
        Assert.Equal(sideNumber, side.SideNumber);
        Assert.Equal(score, side.Score);
        var participant = Assert.Single(side.Participants);
        Assert.Equal(1, participant.SlotNumber);
        Assert.Equal(userId, participant.UserId);
        Assert.False(string.IsNullOrWhiteSpace(participant.MemberDisplayName));
        Assert.Null(participant.GuestName);
    }

    private static void AssertResponseMatchesGame(
        RecordGameHttpResponse response,
        Game game)
    {
        var responseSides = response.Sides.OrderBy(side => side.SideNumber).ToArray();
        var gameSides = game.Sides.OrderBy(side => side.SideNumber).ToArray();

        Assert.Equal(gameSides.Length, responseSides.Length);

        for (var index = 0; index < gameSides.Length; index++)
        {
            Assert.Equal(gameSides[index].SideNumber, responseSides[index].SideNumber);
            Assert.Equal(gameSides[index].Score, responseSides[index].Score);
            Assert.Equal(
                gameSides[index].Participants[0].UserId,
                responseSides[index].Participants[0].UserId
            );
        }
    }

    public enum InvalidGameRequest
    {
        MissingSide,
        RepeatedMember,
        BlankGuestName,
        MemberAndGuestIdentity,
    }
}
