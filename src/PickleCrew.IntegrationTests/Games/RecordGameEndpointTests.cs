using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Games;
using PickleCrew.Api.Games.RecordGame;
using PickleCrew.Api.Services;
using PickleCrew.Api.Statistics;
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
    public async Task RecordGame_WhenFourMembersPlayOneDoublesGame_ShouldPersistPairingsAndReturnNoBestPairing()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: "user_pairing_morgan",
                displayName: "Morgan Kitchen",
                profilePictureUrl: "https://example.test/morgan.jpg"
            )
            .WithMember(
                id: "user_pairing_alex",
                displayName: "Alex Paddle",
                profilePictureUrl: "https://example.test/alex.jpg"
            )
            .WithMember(
                id: "user_pairing_jordan",
                displayName: "Jordan Dink"
            )
            .WithMember(
                id: "user_pairing_taylor",
                displayName: "Taylor Volley"
            )
            .BuildAsync();
        var alex = context.Members[0];
        var jordan = context.Members[1];
        var taylor = context.Members[2];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(alex.UserId)
                ),
                CreateSide(
                    7,
                    CreateMemberParticipant(taylor.UserId),
                    CreateMemberParticipant(jordan.UserId)
                ),
            ],
        };

        using var recordResponse = await apiClient.PostAsJsonAsync(
            "/games",
            request,
            TestJsonSerializerOptions.Default
        );

        recordResponse.EnsureSuccessStatusCode();

        await using (var database = fixture.CreateDbContext())
        {
            var pairingStatistics = await database.CrewDoublesPairingStatistics
                .AsNoTracking()
                .Where(statistics => statistics.CrewUuid == context.Uuid)
                .ToArrayAsync();

            Assert.Equal(2, pairingStatistics.Length);
            AssertPairingStatistics(
                Assert.Single(pairingStatistics, statistics =>
                    statistics.Player1Id == alex.UserId
                    && statistics.Player2Id == context.Administrator.UserId
                ),
                context.Uuid,
                wins: 1,
                losses: 0,
                pointsScored: 11,
                pointsConceded: 7
            );
            AssertPairingStatistics(
                Assert.Single(pairingStatistics, statistics =>
                    statistics.Player1Id == jordan.UserId
                    && statistics.Player2Id == taylor.UserId
                ),
                context.Uuid,
                wins: 0,
                losses: 1,
                pointsScored: 7,
                pointsConceded: 11
            );
        }

        using var pairingResponse = await apiClient.GetAsync(
            "/statistics/best-doubles-pairing"
        );

        pairingResponse.EnsureSuccessStatusCode();
        var result = await pairingResponse.Content.ReadFromJsonAsync<GetBestDoublesPairingHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Null(result.Pairing);
    }

    [Fact]
    public async Task RecordGame_WhenPairingOrderIsReversed_ShouldAccumulateOneCanonicalPairingPerSide()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var firstTeammate = context.Members[0];
        var firstOpponent = context.Members[1];
        var secondOpponent = context.Members[2];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var firstRequest = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(firstTeammate.UserId)
                ),
                CreateSide(
                    7,
                    CreateMemberParticipant(firstOpponent.UserId),
                    CreateMemberParticipant(secondOpponent.UserId)
                ),
            ],
        };
        var reversedRequest = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    9,
                    CreateMemberParticipant(firstTeammate.UserId),
                    CreateMemberParticipant(context.Administrator.UserId)
                ),
                CreateSide(
                    11,
                    CreateMemberParticipant(secondOpponent.UserId),
                    CreateMemberParticipant(firstOpponent.UserId)
                ),
            ],
        };

        using var firstResponse = await apiClient.PostAsJsonAsync(
            "/games",
            firstRequest,
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await apiClient.PostAsJsonAsync(
            "/games",
            reversedRequest,
            TestJsonSerializerOptions.Default
        );

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        await using var database = fixture.CreateDbContext();
        var pairingStatistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewUuid == context.Uuid)
            .ToArrayAsync();
        var firstPairingKey = CrewDoublesPairingKey.Create(
            context.Administrator.UserId,
            firstTeammate.UserId
        );
        var opposingPairingKey = CrewDoublesPairingKey.Create(
            firstOpponent.UserId,
            secondOpponent.UserId
        );

        Assert.Equal(2, pairingStatistics.Length);
        AssertPairingStatistics(
            Assert.Single(pairingStatistics, statistics =>
                statistics.Player1Id == firstPairingKey.Player1Id
                && statistics.Player2Id == firstPairingKey.Player2Id
            ),
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 20,
            pointsConceded: 18
        );
        AssertPairingStatistics(
            Assert.Single(pairingStatistics, statistics =>
                statistics.Player1Id == opposingPairingKey.Player1Id
                && statistics.Player2Id == opposingPairingKey.Player2Id
            ),
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 18,
            pointsConceded: 20
        );
    }

    [Fact]
    public async Task RecordGame_WhenMemberPairingPlaysAgainstGuestSide_ShouldPersistOnlyMemberPairing()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var teammate = context.Members[0];
        var opponent = context.Members[1];
        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var request = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(teammate.UserId)
                ),
                CreateSide(
                    8,
                    CreateMemberParticipant(opponent.UserId),
                    CreateGuestParticipant("Jamie Kitchen")
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
        var pairingStatistics = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .SingleAsync(statistics => statistics.CrewUuid == context.Uuid);
        var pairingKey = CrewDoublesPairingKey.Create(
            context.Administrator.UserId,
            teammate.UserId
        );

        Assert.Equal(pairingKey.Player1Id, pairingStatistics.Player1Id);
        Assert.Equal(pairingKey.Player2Id, pairingStatistics.Player2Id);
        AssertPairingStatistics(
            pairingStatistics,
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 8
        );
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
        Assert.Equal(
            0,
            await dbContext.CrewDoublesPairingStatistics.CountAsync(candidate =>
                candidate.CrewId == crewId
            )
        );

        var winnerStatistics = statistics[context.Administrator.UserId];
        Assert.Equal(1, winnerStatistics.CurrentWinStreak);
        Assert.Equal(game.PlayedAtUtc, winnerStatistics.CurrentWinStreakStartedAtUtc);
        Assert.Equal(1, winnerStatistics.LongestWinStreak);
        Assert.Equal(game.PlayedAtUtc, winnerStatistics.LongestWinStreakStartedAtUtc);
        Assert.Null(winnerStatistics.LongestWinStreakEndedAtUtc);
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
    public async Task RecordGame_WhenResultsContainMultipleStreaks_ShouldPersistCurrentAndLongestWinStreakPeriods()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
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
            using var response = await apiClient.PostAsJsonAsync(
                "/games",
                CreateRequest(
                    context.Administrator.UserId,
                    opponent.UserId,
                    administratorScore,
                    opponentScore
                ),
                TestJsonSerializerOptions.Default
            );
            response.EnsureSuccessStatusCode();
        }

        await using var database = fixture.CreateDbContext();
        var games = await database.Games
            .AsNoTracking()
            .Where(game => game.CrewUuid == context.Uuid)
            .OrderBy(game => game.Id)
            .ToArrayAsync();
        var statistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewUuid == context.Uuid &&
                candidate.UserId == context.Administrator.UserId
            );

        Assert.Equal(2, statistics.CurrentWinStreak);
        Assert.Equal(games[4].PlayedAtUtc, statistics.CurrentWinStreakStartedAtUtc);
        Assert.Equal(3, statistics.LongestWinStreak);
        Assert.Equal(games[0].PlayedAtUtc, statistics.LongestWinStreakStartedAtUtc);
        Assert.Equal(games[3].PlayedAtUtc, statistics.LongestWinStreakEndedAtUtc);
    }

    [Fact]
    public async Task RecordGame_WhenActiveStreakExceedsEndedLongestStreak_ShouldReplaceLongestWinStreakPeriod()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .BuildAsync();
        var opponent = context.Members[0];
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
            using var response = await apiClient.PostAsJsonAsync(
                "/games",
                CreateRequest(
                    context.Administrator.UserId,
                    opponent.UserId,
                    administratorScore,
                    opponentScore
                ),
                TestJsonSerializerOptions.Default
            );
            response.EnsureSuccessStatusCode();
        }

        await using var database = fixture.CreateDbContext();
        var games = await database.Games
            .AsNoTracking()
            .Where(game => game.CrewUuid == context.Uuid)
            .OrderBy(game => game.Id)
            .ToArrayAsync();
        var statistics = await database.CrewMemberStatistics
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewUuid == context.Uuid &&
                candidate.UserId == context.Administrator.UserId
            );

        Assert.Equal(3, statistics.CurrentWinStreak);
        Assert.Equal(games[3].PlayedAtUtc, statistics.CurrentWinStreakStartedAtUtc);
        Assert.Equal(3, statistics.LongestWinStreak);
        Assert.Equal(games[3].PlayedAtUtc, statistics.LongestWinStreakStartedAtUtc);
        Assert.Null(statistics.LongestWinStreakEndedAtUtc);
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
        Assert.Equal(
            0,
            await dbContext.CrewDoublesPairingStatistics.CountAsync(candidate =>
                candidate.CrewUuid == context.Uuid
            )
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
    public async Task RecordGame_WhenSamePairingPlaysInTwoCrews_ShouldKeepPairingStatisticsSeparate()
    {
        var firstPlayerId = $"user_test_{Guid.NewGuid():N}";
        var secondPlayerId = $"user_test_{Guid.NewGuid():N}";
        var firstContext = await fixture.CreateCrewBuilder()
            .WithName("First Pairing Crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        var secondContext = await fixture.CreateCrewBuilder()
            .WithName("Second Pairing Crew")
            .WithMember(id: firstPlayerId)
            .WithMember(id: secondPlayerId)
            .BuildAsync();
        using var firstApiClient = fixture.CreateApiClient(firstContext.Administrator);
        using var secondApiClient = fixture.CreateApiClient(secondContext.Administrator);
        var firstRequest = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(firstPlayerId),
                    CreateMemberParticipant(secondPlayerId)
                ),
                CreateSide(
                    5,
                    CreateGuestParticipant("First Guest"),
                    CreateGuestParticipant("Second Guest")
                ),
            ],
        };
        var secondRequest = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    7,
                    CreateMemberParticipant(secondPlayerId),
                    CreateMemberParticipant(firstPlayerId)
                ),
                CreateSide(
                    11,
                    CreateGuestParticipant("Third Guest"),
                    CreateGuestParticipant("Fourth Guest")
                ),
            ],
        };

        using var firstResponse = await firstApiClient.PostAsJsonAsync(
            "/games",
            firstRequest,
            TestJsonSerializerOptions.Default
        );
        using var secondResponse = await secondApiClient.PostAsJsonAsync(
            "/games",
            secondRequest,
            TestJsonSerializerOptions.Default
        );

        firstResponse.EnsureSuccessStatusCode();
        secondResponse.EnsureSuccessStatusCode();

        await using var database = fixture.CreateDbContext();
        var pairingKey = CrewDoublesPairingKey.Create(firstPlayerId, secondPlayerId);
        var statisticsByCrew = await database.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics =>
                statistics.Player1Id == pairingKey.Player1Id
                && statistics.Player2Id == pairingKey.Player2Id
            )
            .ToDictionaryAsync(statistics => statistics.CrewUuid);

        Assert.Equal(2, statisticsByCrew.Count);
        AssertPairingStatistics(
            statisticsByCrew[firstContext.Uuid],
            firstContext.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 5
        );
        AssertPairingStatistics(
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
                CreateMemberStatistics(
                    crewId,
                    context.Uuid,
                    winnerUserId,
                    wins: 2,
                    losses: 1,
                    pointsScored: 31,
                    pointsConceded: 20
                ),
                CreateMemberStatistics(
                    crewId,
                    context.Uuid,
                    loserUserId,
                    wins: 1,
                    losses: 2,
                    pointsScored: 20,
                    pointsConceded: 31
                )
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
        var statisticsChanges = await statisticsService.CalculateStatisticsAsync(
            game,
            CancellationToken.None
        );
        Assert.Equal(2, statisticsChanges.PlayerStatistics.Length);
        Assert.All(
            statisticsChanges.PlayerStatistics,
            change => Assert.Equal(
                CalculateGameStatisticsResultOperation.Updated,
                change.Operation
            )
        );
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

    [Fact]
    public async Task SaveChanges_WhenPairingStatisticsHaveConcurrencyConflict_ShouldRollBackAllGameChanges()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var firstTeammateId = context.Members[0].UserId;
        var firstOpponentId = context.Members[1].UserId;
        var secondOpponentId = context.Members[2].UserId;
        var firstPairingKey = CrewDoublesPairingKey.Create(
            context.Administrator.UserId,
            firstTeammateId
        );
        var opposingPairingKey = CrewDoublesPairingKey.Create(
            firstOpponentId,
            secondOpponentId
        );

        using var apiClient = fixture.CreateApiClient(context.Administrator);
        var seedRequest = new RecordGameHttpRequest
        {
            Format = GameFormat.Doubles,
            Sides =
            [
                CreateSide(
                    11,
                    CreateMemberParticipant(context.Administrator.UserId),
                    CreateMemberParticipant(firstTeammateId)
                ),
                CreateSide(
                    7,
                    CreateMemberParticipant(firstOpponentId),
                    CreateMemberParticipant(secondOpponentId)
                ),
            ],
        };
        using var seedResponse = await apiClient.PostAsJsonAsync(
            "/games",
            seedRequest,
            TestJsonSerializerOptions.Default
        );
        seedResponse.EnsureSuccessStatusCode();

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
        var game = new Game
        {
            CrewId = context.Id,
            CrewUuid = context.Uuid,
            Format = GameFormat.Doubles,
            RecorderUserId = context.Administrator.UserId,
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
                            UserId = context.Administrator.UserId,
                        },
                        new GameParticipant
                        {
                            SlotNumber = 2,
                            UserId = firstTeammateId,
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
                            UserId = firstOpponentId,
                        },
                        new GameParticipant
                        {
                            SlotNumber = 2,
                            UserId = secondOpponentId,
                        },
                    ],
                },
            ],
        };
        var statisticsService = new GameStatisticsService(staleDatabase);
        var statisticsChanges = await statisticsService.CalculateStatisticsAsync(
            game,
            CancellationToken.None
        );
        Assert.Equal(4, statisticsChanges.PlayerStatistics.Length);
        Assert.All(
            statisticsChanges.PlayerStatistics,
            change => Assert.Equal(
                CalculateGameStatisticsResultOperation.Updated,
                change.Operation
            )
        );
        Assert.Equal(2, statisticsChanges.DoublesPairingStatistics.Length);
        Assert.All(
            statisticsChanges.DoublesPairingStatistics,
            change => Assert.Equal(
                CalculateGameStatisticsResultOperation.Updated,
                change.Operation
            )
        );
        staleDatabase.Games.Add(game);

        await using (var competingDatabase = fixture.CreateDbContext())
        {
            var competingStatistics = await competingDatabase.CrewDoublesPairingStatistics
                .SingleAsync(statistics =>
                    statistics.CrewId == context.Id
                    && statistics.Player1Id == firstPairingKey.Player1Id
                    && statistics.Player2Id == firstPairingKey.Player2Id
                );
            competingStatistics.Losses += 1;
            competingStatistics.PointsScored += 7;
            competingStatistics.PointsConceded += 11;
            await competingDatabase.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            async () => await staleDatabase.SaveChangesAsync()
        );

        await using var verificationDatabase = fixture.CreateDbContext();
        Assert.Equal(
            1,
            await verificationDatabase.Games.CountAsync(gameCandidate =>
                gameCandidate.CrewId == context.Id
            )
        );
        var persistedMemberStatistics = await verificationDatabase.CrewMemberStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == context.Id)
            .ToDictionaryAsync(statistics => statistics.UserId);
        Assert.Equal(4, persistedMemberStatistics.Count);
        AssertStatistics(
            persistedMemberStatistics[context.Administrator.UserId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7
        );
        AssertStatistics(
            persistedMemberStatistics[firstTeammateId],
            context.Uuid,
            wins: 1,
            losses: 0,
            pointsScored: 11,
            pointsConceded: 7
        );
        AssertStatistics(
            persistedMemberStatistics[firstOpponentId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
        AssertStatistics(
            persistedMemberStatistics[secondOpponentId],
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
        );
        var persistedPairingStatistics = await verificationDatabase.CrewDoublesPairingStatistics
            .AsNoTracking()
            .Where(statistics => statistics.CrewId == context.Id)
            .ToArrayAsync();
        AssertPairingStatistics(
            Assert.Single(persistedPairingStatistics, statistics =>
                statistics.Player1Id == firstPairingKey.Player1Id
                && statistics.Player2Id == firstPairingKey.Player2Id
            ),
            context.Uuid,
            wins: 1,
            losses: 1,
            pointsScored: 18,
            pointsConceded: 18
        );
        AssertPairingStatistics(
            Assert.Single(persistedPairingStatistics, statistics =>
                statistics.Player1Id == opposingPairingKey.Player1Id
                && statistics.Player2Id == opposingPairingKey.Player2Id
            ),
            context.Uuid,
            wins: 0,
            losses: 1,
            pointsScored: 7,
            pointsConceded: 11
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
        Assert.Equal(
            0,
            await dbContext.CrewDoublesPairingStatistics.CountAsync(statistics =>
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

    private static void AssertPairingStatistics(
        CrewDoublesPairingStatistics statistics,
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

    private static CrewMemberStatistics CreateMemberStatistics(
        long crewId,
        Guid crewUuid,
        string userId,
        long wins,
        long losses,
        long pointsScored,
        long pointsConceded)
    {
        var statistics = new CrewMemberStatistics
        {
            CrewId = crewId,
            CrewUuid = crewUuid,
            UserId = userId,
            Wins = wins,
            Losses = losses,
            PointsScored = pointsScored,
            PointsConceded = pointsConceded,
        };
        return statistics;
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

        var gamesPlayed = wins + losses;
        var pointDifferential = pointsScored - pointsConceded;

        Assert.Equal(gamesPlayed, statistics.GamesPlayed);
        Assert.Equal(
            gamesPlayed == 0 ? 0 : (decimal)wins / gamesPlayed,
            statistics.WinPercentage
        );
        Assert.Equal(pointDifferential, statistics.PointDifferential);
        Assert.Equal((wins * 100) + pointDifferential, statistics.CrewScore);
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
