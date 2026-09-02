using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Games;
using PickleCrew.Api.Games.RecordGame;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics.Services;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class RecordGameEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task RecordGame_WhenFourMembersPlayDoubles_ShouldPersistCompleteGameBasedRecords()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var teammateId = context.Members[0].UserId;
        var firstOpponentId = context.Members[1].UserId;
        var secondOpponentId = context.Members[2].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(teammateId)
                ),
                CreateSide(
                    0,
                    CreateMemberParticipant(firstOpponentId),
                    CreateMemberParticipant(secondOpponentId)
                ),
            ],
        };

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        response.EnsureSuccessStatusCode();
        var recordedGame = await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(recordedGame);
        Assert.Equal(context.Uuid, recordedGame.CrewId);
        Assert.Equal(context.Administrator.UserId, recordedGame.RecorderUserId);
        Assert.Equal(GameFormat.Doubles, recordedGame.Format);
        Assert.Collection(
            recordedGame.Sides.OrderBy(side => side.SideNumber),
            side =>
            {
                Assert.Equal(11, side.Score);
                Assert.Equal(2, side.Participants.Length);
            },
            side =>
            {
                Assert.Equal(0, side.Score);
                Assert.Equal(2, side.Participants.Length);
            }
        );

        await using var database = fixture.CreateDbContext();
        var game = await database.Games
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Uuid == recordedGame.GameId);
        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.Uuid)
            .ToDictionaryAsync(statistics => statistics.UserId);
        var matchupStatistics = await database.MatchupStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.Uuid)
            .ToArrayAsync();
        var pairingStatistics = await database.DoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.Uuid)
            .ToArrayAsync();
        var winStreaks = await database.PlayerWinStreaks
            .AsNoTracking()
            .Where(winStreak => winStreak.CrewUuid == context.Uuid)
            .ToDictionaryAsync(winStreak => winStreak.UserId);

        Assert.Equal(4, memberStatistics.Count);
        Assert.Equal(8, matchupStatistics.Length);
        Assert.Equal(2, pairingStatistics.Length);
        Assert.Equal(4, winStreaks.Count);

        var winnerIds = new[]
        {
            context.Administrator.UserId,
            teammateId,
        };
        var loserIds = new[]
        {
            firstOpponentId,
            secondOpponentId,
        };

        foreach (var winnerId in winnerIds)
        {
            AssertMemberGameStatistics(
                memberStatistics[winnerId],
                game,
                wins: 1,
                losses: 0,
                pointsScored: 11,
                pointsConceded: 0,
                crewScore: 111,
                thatsAPaddlin: 1
            );
            AssertGameBasedWinStreak(
                winStreaks[winnerId],
                game,
                won: true
            );
        }

        foreach (var loserId in loserIds)
        {
            AssertMemberGameStatistics(
                memberStatistics[loserId],
                game,
                wins: 0,
                losses: 1,
                pointsScored: 0,
                pointsConceded: 11,
                crewScore: 0,
                thatsAPaddlin: 0
            );
            AssertGameBasedWinStreak(
                winStreaks[loserId],
                game,
                won: false
            );
        }

        foreach (var winnerId in winnerIds)
        {
            foreach (var loserId in loserIds)
            {
                AssertMatchupGameStatistics(
                    Assert.Single(matchupStatistics, statistics =>
                        statistics.UserId == winnerId
                        && statistics.OpponentUserId == loserId
                    ),
                    game,
                    wins: 1,
                    losses: 0,
                    pointsScored: 11,
                    pointsConceded: 0
                );
                AssertMatchupGameStatistics(
                    Assert.Single(matchupStatistics, statistics =>
                        statistics.UserId == loserId
                        && statistics.OpponentUserId == winnerId
                    ),
                    game,
                    wins: 0,
                    losses: 1,
                    pointsScored: 0,
                    pointsConceded: 11
                );
            }
        }

        AssertPairingGameStatistics(
            pairingStatistics,
            game,
            context.Administrator.UserId,
            teammateId,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 0
        );
        AssertPairingGameStatistics(
            pairingStatistics,
            game,
            firstOpponentId,
            secondOpponentId,
            wins: 0,
            losses: 1,
            pointsScored: 0,
            pointsConceded: 11
        );
    }

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

    [Fact]
    public async Task RecordGame_WhenIdenticalGameIsRecordedTwice_ShouldPersistBothGamesAndSeparateContributions()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponentId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = CreateSinglesRequest(
            context.Administrator.UserId,
            opponentId,
            11,
            7
        );

        var firstGame = await RecordGameAsync(apiClient, request);
        var secondGame = await RecordGameAsync(apiClient, request);

        await using var database = fixture.CreateDbContext();
        Assert.NotEqual(firstGame.GameId, secondGame.GameId);
        Assert.Equal(2, await database.Games.CountAsync(game =>
            game.CrewUuid == context.Uuid
        ));
        Assert.Equal(4, await database.PlayerStatistics.CountAsync(statistics =>
            statistics.CrewUuid == context.Uuid
        ));
        Assert.Equal(4, await database.MatchupStatistics.CountAsync(statistics =>
            statistics.CrewUuid == context.Uuid
        ));
        Assert.Equal(2, await database.PlayerStatistics.CountAsync(statistics =>
            statistics.GameUuid == firstGame.GameId
        ));
        Assert.Equal(2, await database.PlayerStatistics.CountAsync(statistics =>
            statistics.GameUuid == secondGame.GameId
        ));
    }

    [Fact]
    public async Task RecordGame_WhenActiveRecorderDidNotPlay_ShouldPersistOnlyParticipantContributions()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var firstParticipantId = context.Members[0].UserId;
        var secondParticipantId = context.Members[1].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var game = await RecordGameAsync(
            apiClient,
            CreateSinglesRequest(
                firstParticipantId,
                secondParticipantId,
                12,
                10
            )
        );

        Assert.Equal(context.Administrator.UserId, game.RecorderUserId);
        Assert.DoesNotContain(
            game.Sides.SelectMany(side => side.Participants),
            participant => participant.UserId == context.Administrator.UserId
        );

        await using var database = fixture.CreateDbContext();
        var memberIds = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.GameId)
            .Select(statistics => statistics.UserId)
            .ToArrayAsync();
        var winStreakMemberIds = await database.PlayerWinStreaks
            .AsNoTracking()
            .Where(winStreak => winStreak.CrewUuid == context.Uuid)
            .Select(winStreak => winStreak.UserId)
            .ToArrayAsync();

        Assert.Equal(2, memberIds.Length);
        Assert.Contains(firstParticipantId, memberIds);
        Assert.Contains(secondParticipantId, memberIds);
        Assert.DoesNotContain(context.Administrator.UserId, memberIds);
        Assert.DoesNotContain(context.Administrator.UserId, winStreakMemberIds);
    }

    [Fact]
    public async Task RecordGame_WhenSinglesIncludesGuest_ShouldPersistGuestAndOnlyMemberStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Singles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId)
                ),
                CreateSide(9, CreateGuestParticipant("  Jamie Kitchen  ")),
            ],
        };

        var recordedGame = await RecordGameAsync(apiClient, request);

        Assert.Equal("Jamie Kitchen", recordedGame.Sides[1].Participants[0].GuestName);
        Assert.Null(recordedGame.Sides[1].Participants[0].UserId);

        await using var database = fixture.CreateDbContext();
        var game = await database.Games
            .AsNoTracking()
            .SingleAsync(candidate => candidate.Uuid == recordedGame.GameId);
        var guest = game.Sides
            .SelectMany(side => side.Participants)
            .Single(participant => participant.GuestName is not null);

        Assert.Equal("Jamie Kitchen", guest.GuestName);
        Assert.Equal(1, await database.PlayerStatistics.CountAsync(statistics =>
            statistics.GameUuid == recordedGame.GameId
        ));
        Assert.Equal(0, await database.MatchupStatistics.CountAsync(statistics =>
            statistics.GameUuid == recordedGame.GameId
        ));
        Assert.Equal(1, await database.PlayerWinStreaks.CountAsync(winStreak =>
            winStreak.CrewUuid == context.Uuid
        ));
    }

    [Fact]
    public async Task RecordGame_WhenResultsContainMultipleStreaks_ShouldPersistCurrentAndLongestWinStreakPeriods()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponentId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var scores = new[]
        {
            (11, 7),
            (11, 8),
            (11, 9),
            (9, 11),
            (11, 6),
            (11, 5),
        };

        foreach (var (administratorScore, opponentScore) in scores)
        {
            await RecordGameAsync(
                apiClient,
                CreateSinglesRequest(
                    context.Administrator.UserId,
                    opponentId,
                    administratorScore,
                    opponentScore
                )
            );
        }

        await using var database = fixture.CreateDbContext();
        var games = await database.Games
            .AsNoTracking()
            .Where(game => game.CrewUuid == context.Uuid)
            .OrderBy(game => game.Id)
            .ToArrayAsync();
        var winStreak = await database.PlayerWinStreaks
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewUuid == context.Uuid
                && candidate.UserId == context.Administrator.UserId
            );

        Assert.Equal(2, winStreak.CurrentWinStreak);
        Assert.Equal(games[4].PlayedAtUtc, winStreak.CurrentWinStreakStartedAtUtc);
        Assert.Equal(3, winStreak.LongestWinStreak);
        Assert.Equal(games[0].PlayedAtUtc, winStreak.LongestWinStreakStartedAtUtc);
        Assert.Equal(games[3].PlayedAtUtc, winStreak.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task RecordGame_WhenActiveStreakExceedsEndedLongestStreak_ShouldReplaceLongestWinStreakPeriod()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponentId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var scores = new[]
        {
            (11, 7),
            (11, 8),
            (9, 11),
            (11, 6),
            (11, 5),
            (11, 4),
        };

        foreach (var (administratorScore, opponentScore) in scores)
        {
            await RecordGameAsync(
                apiClient,
                CreateSinglesRequest(
                    context.Administrator.UserId,
                    opponentId,
                    administratorScore,
                    opponentScore
                )
            );
        }

        await using var database = fixture.CreateDbContext();
        var games = await database.Games
            .AsNoTracking()
            .Where(game => game.CrewUuid == context.Uuid)
            .OrderBy(game => game.Id)
            .ToArrayAsync();
        var winStreak = await database.PlayerWinStreaks
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewUuid == context.Uuid
                && candidate.UserId == context.Administrator.UserId
            );

        Assert.Equal(3, winStreak.CurrentWinStreak);
        Assert.Equal(games[3].PlayedAtUtc, winStreak.CurrentWinStreakStartedAtUtc);
        Assert.Equal(3, winStreak.LongestWinStreak);
        Assert.Equal(games[3].PlayedAtUtc, winStreak.LongestWinStreakStartedAtUtc);
        Assert.Null(winStreak.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task RecordGame_WhenMultipleFormatsAreRecorded_ShouldPersistSeparateRecordsAtEachGameGrain()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var firstMemberId = context.Members[0].UserId;
        var secondMemberId = context.Members[1].UserId;
        var thirdMemberId = context.Members[2].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var singlesGame = await RecordGameAsync(
            apiClient,
            CreateSinglesRequest(
                context.Administrator.UserId,
                firstMemberId,
                11,
                7
            )
        );
        var doublesGame = await RecordGameAsync(
            apiClient,
            CreateDoublesRequest(
                9,
                [context.Administrator.UserId, firstMemberId],
                11,
                [secondMemberId, thirdMemberId]
            )
        );

        await using var database = fixture.CreateDbContext();
        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .ToArrayAsync();
        var matchupStatistics = await database.MatchupStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .ToArrayAsync();
        var pairingStatistics = await database.DoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .ToArrayAsync();
        var winStreaks = await database.PlayerWinStreaks
            .AsNoTracking()
            .Where(winStreak => winStreak.CrewUuid == context.Uuid)
            .ToArrayAsync();

        Assert.Equal(6, memberStatistics.Length);
        Assert.Equal(10, matchupStatistics.Length);
        Assert.Equal(2, pairingStatistics.Length);
        Assert.Equal(4, winStreaks.Length);
        Assert.Equal(
            2,
            memberStatistics.Count(statistics => statistics.GameUuid == singlesGame.GameId)
        );
        Assert.Equal(
            4,
            memberStatistics.Count(statistics => statistics.GameUuid == doublesGame.GameId)
        );
        Assert.Equal(
            2,
            matchupStatistics.Count(statistics => statistics.GameUuid == singlesGame.GameId)
        );
        Assert.Equal(
            8,
            matchupStatistics.Count(statistics => statistics.GameUuid == doublesGame.GameId)
        );
        Assert.DoesNotContain(
            pairingStatistics,
            statistics => statistics.GameUuid == singlesGame.GameId
        );
        Assert.All(
            pairingStatistics,
            statistics => Assert.Equal(doublesGame.GameId, statistics.GameUuid)
        );
        Assert.All(
            memberStatistics,
            statistics => Assert.Equal(context.Id, statistics.CrewId)
        );
        Assert.All(
            matchupStatistics,
            statistics => Assert.Equal(context.Uuid, statistics.CrewUuid)
        );
    }

    [Fact]
    public async Task RecordGame_WhenBothDoublesSidesContainGuests_ShouldPersistOnlyMemberContributionsAndMatchups()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponentId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var game = await RecordGameAsync(
            apiClient,
            CreateDoublesRequest(
                13,
                [context.Administrator.UserId, null],
                11,
                [opponentId, null],
                firstGuestName: "Winning guest",
                secondGuestName: "Losing guest"
            )
        );

        await using var database = fixture.CreateDbContext();
        var memberStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.GameId)
            .ToDictionaryAsync(statistics => statistics.UserId);
        var matchupStatistics = await database.MatchupStatistics
            .AsNoTracking()
            .Where(statistics => statistics.GameUuid == game.GameId)
            .ToArrayAsync();
        var winStreaks = await database.PlayerWinStreaks
            .AsNoTracking()
            .Where(winStreak => winStreak.CrewUuid == context.Uuid)
            .ToDictionaryAsync(winStreak => winStreak.UserId);

        Assert.Equal(2, memberStatistics.Count);
        Assert.Equal(2, matchupStatistics.Length);
        Assert.Equal(2, winStreaks.Count);
        Assert.Empty(
            await database.DoublesPairingStatistics
                .AsNoTracking()
                .Where(statistics => statistics.GameUuid == game.GameId)
                .ToArrayAsync()
        );
        Assert.Equal(1, memberStatistics[context.Administrator.UserId].Wins);
        Assert.Equal(1, memberStatistics[opponentId].Losses);
        Assert.Contains(matchupStatistics, statistics =>
            statistics.UserId == context.Administrator.UserId
            && statistics.OpponentUserId == opponentId
        );
        Assert.Contains(matchupStatistics, statistics =>
            statistics.UserId == opponentId
            && statistics.OpponentUserId == context.Administrator.UserId
        );
    }

    [Fact]
    public async Task RecordGame_WhenOnlyOpposingSideContainsGuest_ShouldPersistTheMemberPairing()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var teammateId = context.Members[0].UserId;
        var opponentId = context.Members[1].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var game = await RecordGameAsync(
            apiClient,
            CreateDoublesRequest(
                11,
                [context.Administrator.UserId, teammateId],
                8,
                [opponentId, null],
                secondGuestName: "Guest opponent"
            )
        );

        await using var database = fixture.CreateDbContext();
        var pairing = await database.DoublesPairingStatistics
            .AsNoTracking()
            .SingleAsync(statistics => statistics.GameUuid == game.GameId);
        var key = CrewDoublesPairingKey.Create(
            context.Administrator.UserId,
            teammateId
        );

        Assert.Equal(key.Player1Id, pairing.Player1Id);
        Assert.Equal(key.Player2Id, pairing.Player2Id);
        Assert.Equal(1, pairing.Wins);
        Assert.Equal(0, pairing.Losses);
        Assert.Equal(11, pairing.PointsScored);
        Assert.Equal(8, pairing.PointsConceded);
        Assert.Equal(3, await database.PlayerStatistics.CountAsync(statistics =>
            statistics.GameUuid == game.GameId
        ));
        Assert.Equal(4, await database.MatchupStatistics.CountAsync(statistics =>
            statistics.GameUuid == game.GameId
        ));
        Assert.Equal(3, await database.PlayerWinStreaks.CountAsync(winStreak =>
            winStreak.CrewUuid == context.Uuid
        ));
    }

    [Fact]
    public async Task RecordGame_WhenPairingOrderIsReversed_ShouldPersistCanonicalPairingPerGame()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var teammateId = context.Members[0].UserId;
        var firstOpponentId = context.Members[1].UserId;
        var secondOpponentId = context.Members[2].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var firstGame = await RecordGameAsync(
            apiClient,
            CreateDoublesRequest(
                11,
                [context.Administrator.UserId, teammateId],
                7,
                [firstOpponentId, secondOpponentId]
            )
        );
        var secondGame = await RecordGameAsync(
            apiClient,
            CreateDoublesRequest(
                9,
                [teammateId, context.Administrator.UserId],
                11,
                [secondOpponentId, firstOpponentId]
            )
        );

        await using var database = fixture.CreateDbContext();
        var pairings = await database.DoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .ToArrayAsync();
        var firstKey = CrewDoublesPairingKey.Create(
            context.Administrator.UserId,
            teammateId
        );
        var secondKey = CrewDoublesPairingKey.Create(
            firstOpponentId,
            secondOpponentId
        );

        Assert.Equal(4, pairings.Length);
        Assert.Equal(2, pairings.Count(statistics =>
            statistics.Player1Id == firstKey.Player1Id
            && statistics.Player2Id == firstKey.Player2Id
        ));
        Assert.Equal(2, pairings.Count(statistics =>
            statistics.Player1Id == secondKey.Player1Id
            && statistics.Player2Id == secondKey.Player2Id
        ));
        Assert.Equal(
            [firstGame.GameId, secondGame.GameId],
            pairings
                .Where(statistics => statistics.Player1Id == firstKey.Player1Id)
                .OrderBy(statistics => statistics.PlayedAtUtc)
                .ThenBy(statistics => statistics.GameId)
                .Select(statistics => statistics.GameUuid)
                .ToArray()
        );
        Assert.All(pairings, statistics => Assert.True(
            string.CompareOrdinal(statistics.Player1Id, statistics.Player2Id) < 0
        ));
    }

    [Fact]
    public async Task RecordGame_WhenSamePlayersPlayInTwoCrews_ShouldKeepGameBasedRecordsIsolatedByCrewAndGame()
    {
        var firstPlayerId = $"user_test_{Guid.NewGuid():N}";
        var secondPlayerId = $"user_test_{Guid.NewGuid():N}";
        var firstCrew = await fixture.CreateCrewBuilder()
            .WithName("First game-based crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        var secondCrew = await fixture.CreateCrewBuilder()
            .WithName("Second game-based crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        using var firstApiClient = fixture.CreateApiClient(firstCrew.Administrator);
        using var secondApiClient = fixture.CreateApiClient(secondCrew.Administrator);

        var firstGame = await RecordGameAsync(
            firstApiClient,
            CreateDoublesRequest(
                11,
                [firstPlayerId, secondPlayerId],
                7,
                [firstCrew.Administrator.UserId, null],
                secondGuestName: "First guest"
            )
        );
        var secondGame = await RecordGameAsync(
            secondApiClient,
            CreateDoublesRequest(
                7,
                [secondPlayerId, firstPlayerId],
                11,
                [secondCrew.Administrator.UserId, null],
                secondGuestName: "Second guest"
            )
        );

        await using var database = fixture.CreateDbContext();
        var pairingKey = CrewDoublesPairingKey.Create(firstPlayerId, secondPlayerId);
        var pairings = await database.DoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.Player1Id == pairingKey.Player1Id
                && statistics.Player2Id == pairingKey.Player2Id
            )
            .ToDictionaryAsync(statistics => statistics.CrewUuid);
        var firstPlayerStatistics = await database.PlayerStatistics
            .AsNoTracking()
            .Where(statistics => statistics.UserId == firstPlayerId)
            .ToDictionaryAsync(statistics => statistics.CrewUuid);
        var firstPlayerStreaks = await database.PlayerWinStreaks
            .AsNoTracking()
            .Where(winStreak => winStreak.UserId == firstPlayerId)
            .ToDictionaryAsync(winStreak => winStreak.CrewUuid);

        Assert.Equal(2, pairings.Count);
        Assert.Equal(firstCrew.Id, pairings[firstCrew.Uuid].CrewId);
        Assert.Equal(firstGame.GameId, pairings[firstCrew.Uuid].GameUuid);
        Assert.Equal(secondCrew.Id, pairings[secondCrew.Uuid].CrewId);
        Assert.Equal(secondGame.GameId, pairings[secondCrew.Uuid].GameUuid);
        Assert.Equal(2, firstPlayerStatistics.Count);
        Assert.Equal(firstGame.GameId, firstPlayerStatistics[firstCrew.Uuid].GameUuid);
        Assert.Equal(secondGame.GameId, firstPlayerStatistics[secondCrew.Uuid].GameUuid);
        Assert.Equal(2, firstPlayerStreaks.Count);
        Assert.Equal(firstCrew.Id, firstPlayerStreaks[firstCrew.Uuid].CrewId);
        Assert.Equal(secondCrew.Id, firstPlayerStreaks[secondCrew.Uuid].CrewId);
    }

    [Fact]
    public async Task SaveChanges_WhenGameBasedWinStreakHasConcurrencyConflict_ShouldRollBackGameAndAllStatistics()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var winnerId = context.Administrator.UserId;
        var loserId = context.Members[0].UserId;
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        await RecordGameAsync(
            apiClient,
            CreateSinglesRequest(winnerId, loserId, 11, 7)
        );

        var crewUserContext = new CrewUserContext
        {
            CrewId = context.Id,
            CrewUuid = context.Uuid,
        };
        var options = new DbContextOptionsBuilder<CrewScopedDbContext>()
            .UseNpgsql(fixture.DatabaseConnectionString)
            .Options;
        await using var staleDatabase = new CrewScopedDbContext(
            options,
            crewUserContext
        );
        var uncommittedGame = CreateSinglesGame(
            context.Id,
            context.Uuid,
            winnerId,
            loserId,
            11,
            8
        );
        staleDatabase.Games.Add(uncommittedGame);
        var statisticsService = new GameStatisticsService(staleDatabase);
        await statisticsService.CalculateStatisticsAsync(
            uncommittedGame,
            CancellationToken.None
        );

        await using (var competingDatabase = fixture.CreateDbContext())
        {
            var competingWinStreak = await competingDatabase.PlayerWinStreaks
                .SingleAsync(winStreak =>
                    winStreak.CrewId == context.Id
                    && winStreak.UserId == winnerId
                );
            competingWinStreak.CurrentWinStreak++;
            competingWinStreak.LongestWinStreak++;
            await competingDatabase.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await staleDatabase.SaveChangesAsync()
        );

        await using var verificationDatabase = fixture.CreateDbContext();
        Assert.Equal(1, await verificationDatabase.Games.CountAsync(game =>
            game.CrewId == context.Id
        ));
        Assert.DoesNotContain(
            await verificationDatabase.Games
                .AsNoTracking()
                .Where(game => game.CrewId == context.Id)
                .Select(game => game.Uuid)
                .ToArrayAsync(),
            gameUuid => gameUuid == uncommittedGame.Uuid
        );
        Assert.Equal(2, await verificationDatabase.PlayerStatistics.CountAsync(
            statistics => statistics.CrewId == context.Id
        ));
        Assert.Equal(2, await verificationDatabase.MatchupStatistics.CountAsync(
            statistics => statistics.CrewId == context.Id
        ));
        Assert.False(await verificationDatabase.PlayerStatistics.AnyAsync(
            statistics => statistics.GameUuid == uncommittedGame.Uuid
        ));
        Assert.False(await verificationDatabase.MatchupStatistics.AnyAsync(
            statistics => statistics.GameUuid == uncommittedGame.Uuid
        ));
    }

    private static void AssertMemberGameStatistics(
        PlayerStatistics statistics,
        Game game,
        int wins,
        int losses,
        int pointsScored,
        int pointsConceded,
        int crewScore,
        int thatsAPaddlin
    )
    {
        AssertGameBasedStatisticsIdentities(
            statistics.GameId,
            statistics.GameUuid,
            statistics.CrewId,
            statistics.CrewUuid,
            statistics.PlayedAtUtc,
            game
        );
        Assert.Equal(wins, statistics.Wins);
        Assert.Equal(losses, statistics.Losses);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
        Assert.Equal(pointsScored - pointsConceded, statistics.PointDifferential);
        Assert.Equal(crewScore, statistics.CrewScore);
        Assert.Equal(thatsAPaddlin, statistics.ThatsAPaddlin);
    }

    private static void AssertMatchupGameStatistics(
        MatchupStatistics statistics,
        Game game,
        int wins,
        int losses,
        int pointsScored,
        int pointsConceded
    )
    {
        AssertGameBasedStatisticsIdentities(
            statistics.GameId,
            statistics.GameUuid,
            statistics.CrewId,
            statistics.CrewUuid,
            statistics.PlayedAtUtc,
            game
        );
        Assert.Equal(wins, statistics.Wins);
        Assert.Equal(losses, statistics.Losses);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
        Assert.Equal(pointsScored - pointsConceded, statistics.PointDifferential);
    }

    private static void AssertPairingGameStatistics(
        DoublesPairingStatistics[] allStatistics,
        Game game,
        string firstPlayerId,
        string secondPlayerId,
        int wins,
        int losses,
        int pointsScored,
        int pointsConceded
    )
    {
        var key = CrewDoublesPairingKey.Create(firstPlayerId, secondPlayerId);
        var statistics = Assert.Single(allStatistics, candidate =>
            candidate.Player1Id == key.Player1Id
            && candidate.Player2Id == key.Player2Id
        );

        AssertGameBasedStatisticsIdentities(
            statistics.GameId,
            statistics.GameUuid,
            statistics.CrewId,
            statistics.CrewUuid,
            statistics.PlayedAtUtc,
            game
        );
        Assert.Equal(wins, statistics.Wins);
        Assert.Equal(losses, statistics.Losses);
        Assert.Equal(pointsScored, statistics.PointsScored);
        Assert.Equal(pointsConceded, statistics.PointsConceded);
        Assert.Equal(pointsScored - pointsConceded, statistics.PointDifferential);
    }

    private static void AssertGameBasedStatisticsIdentities(
        long gameId,
        Guid gameUuid,
        long crewId,
        Guid crewUuid,
        DateTimeOffset playedAtUtc,
        Game game
    )
    {
        Assert.Equal(game.Id, gameId);
        Assert.Equal(game.Uuid, gameUuid);
        Assert.Equal(game.CrewId, crewId);
        Assert.Equal(game.CrewUuid, crewUuid);
        Assert.Equal(game.PlayedAtUtc, playedAtUtc);
    }

    private static void AssertGameBasedWinStreak(
        PlayerWinStreak winStreak,
        Game game,
        bool won
    )
    {
        Assert.Equal(game.CrewId, winStreak.CrewId);
        Assert.Equal(game.CrewUuid, winStreak.CrewUuid);
        Assert.Equal(won ? 1 : 0, winStreak.CurrentWinStreak);
        Assert.Equal(
            won ? game.PlayedAtUtc : null,
            winStreak.CurrentWinStreakStartedAtUtc
        );
        Assert.Equal(won ? 1 : 0, winStreak.LongestWinStreak);
        Assert.Equal(
            won ? game.PlayedAtUtc : null,
            winStreak.LongestWinStreakStartedAtUtc
        );
        Assert.Null(winStreak.LongestWinStreakEndedAtUtc);
        Assert.NotEqual(0u, winStreak.Version);
    }

    private async Task AssertNoGameOrStatisticsAsync(Guid crewUuid)
    {
        await using var database = fixture.CreateDbContext();
        var crewId = await database.Crews
            .Where(crew => crew.Uuid == crewUuid)
            .Select(crew => crew.Id)
            .SingleAsync();

        Assert.Equal(0, await database.Games.CountAsync(game => game.CrewId == crewId));
        Assert.Equal(
            0,
            await database.PlayerStatistics.CountAsync(statistics =>
                statistics.CrewId == crewId
            )
        );
        Assert.Equal(
            0,
            await database.DoublesPairingStatistics.CountAsync(statistics =>
                statistics.CrewId == crewId
            )
        );
        Assert.Equal(
            0,
            await database.MatchupStatistics.CountAsync(statistics =>
                statistics.CrewId == crewId
            )
        );
        Assert.Equal(
            0,
            await database.PlayerWinStreaks.CountAsync(winStreak =>
                winStreak.CrewId == crewId
            )
        );
    }

    private static async Task<RecordGameHttpResponse> RecordGameAsync(
        HttpClient apiClient,
        RecordGameHttpRequest request
    )
    {
        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException("The API returned an empty response.");
    }

    private static RecordGameHttpRequest CreateSinglesRequest(
        string firstPlayerId,
        string secondPlayerId,
        int firstScore,
        int secondScore
    )
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

    private static RecordGameHttpRequest CreateRequest(
        string firstPlayerId,
        string secondPlayerId,
        int firstScore,
        int secondScore
    )
    {
        return CreateSinglesRequest(
            firstPlayerId,
            secondPlayerId,
            firstScore,
            secondScore
        );
    }

    private static RecordGameHttpRequest CreateDoublesRequest(
        int firstScore,
        string?[] firstPlayerIds,
        int secondScore,
        string?[] secondPlayerIds,
        string firstGuestName = "First guest",
        string secondGuestName = "Second guest"
    )
    {
        return new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    firstScore,
                    CreateParticipant(firstPlayerIds[0], firstGuestName),
                    CreateParticipant(firstPlayerIds[1], firstGuestName)
                ),
                CreateSide(
                    secondScore,
                    CreateParticipant(secondPlayerIds[0], secondGuestName),
                    CreateParticipant(secondPlayerIds[1], secondGuestName)
                ),
            ],
        };
    }

    private static RecordGameSideHttpRequest CreateSide(
        int score,
        params RecordGameParticipantHttpRequest[] participants
    )
    {
        return new RecordGameSideHttpRequest
        {
            Score = score,
            Participants = participants,
        };
    }

    private static RecordGameParticipantHttpRequest CreateParticipant(
        string? userId,
        string guestName
    )
    {
        return userId is null
            ? new RecordGameParticipantHttpRequest { GuestName = guestName }
            : CreateMemberParticipant(userId);
    }

    private static RecordGameParticipantHttpRequest CreateMemberParticipant(
        string userId
    )
    {
        return new RecordGameParticipantHttpRequest { UserId = userId };
    }

    private static RecordGameParticipantHttpRequest CreateGuestParticipant(
        string guestName
    )
    {
        return new RecordGameParticipantHttpRequest { GuestName = guestName };
    }

    private static Game CreateSinglesGame(
        long crewId,
        Guid crewUuid,
        string winnerId,
        string loserId,
        int winnerScore,
        int loserScore
    )
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

    public enum InvalidGameRequest
    {
        MissingSide,
        RepeatedMember,
        BlankGuestName,
        MemberAndGuestIdentity,
    }
}
