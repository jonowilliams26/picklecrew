using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class RecordGameEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGameRecordingOptions_WhenRecorderIsActive_ShouldReturnCrewRosterWithoutEmails()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var member = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            displayName: "Jamie Kitchen",
            profilePictureUrl: "https://example.test/jamie.jpg"
        );
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
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var opponent = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
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
        Assert.Equal(context.Crew.CrewId, result.CrewId);
        Assert.Equal(context.Administrator.UserId, result.RecorderUserId);
        Assert.Equal(GameFormat.Singles, result.Format);

        await using var dbContext = fixture.CreateDbContext();
        var crewId = await dbContext.Crews
            .Where(crew => crew.Uuid == context.Crew.CrewId)
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
    }

    [Fact]
    public async Task RecordGame_WhenDoublesIncludesMembersAndGuests_ShouldPersistCompleteResult()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var teammate = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
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
    }

    [Fact]
    public async Task RecordGame_WhenSinglesIncludesGuest_ShouldPersistGuest()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
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
    }

    [Fact]
    public async Task RecordGame_WhenEveryParticipantIsGuest_ShouldRejectWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenGuestNamesDifferOnlyByCaseAndWhitespace_ShouldRejectWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenMemberBelongsToAnotherCrew_ShouldRejectWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var otherCrewMember = TestIdentity.Create();
        await fakeWorkOs.CreateUserAsync(
            otherCrewMember.UserId,
            otherCrewMember.EmailAddress
        );
        await fakeWorkOs.CreateOrganizationAsync(
            otherCrewMember.OrganizationId!,
            "Other Crew",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await fakeWorkOs.CreateMembershipAsync(
            $"membership_test_{Guid.NewGuid():N}",
            otherCrewMember.UserId,
            otherCrewMember.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenIdenticalGameIsRecordedTwice_ShouldPersistBothGames()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var opponent = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
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
        Assert.Equal(2, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenActiveRecorderDidNotPlay_ShouldPersistRecorder()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var firstParticipant = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
        var secondParticipant = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
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
    }

    [Theory]
    [InlineData(InvalidGameRequest.MissingSide)]
    [InlineData(InvalidGameRequest.RepeatedMember)]
    [InlineData(InvalidGameRequest.BlankGuestName)]
    [InlineData(InvalidGameRequest.MemberAndGuestIdentity)]
    public async Task RecordGame_WhenRequestIsStructurallyInvalid_ShouldRejectWithoutWritingGame(
        InvalidGameRequest invalidRequest)
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenParticipantIsInactive_ShouldRejectWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var inactiveParticipant = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            status: FakeMembershipStatus.Inactive
        );
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenRecorderIsInactive_ShouldReturnForbiddenWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeParticipant = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
        var inactiveRecorder = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId,
            status: FakeMembershipStatus.Inactive
        );
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenScoreIsInvalid_ShouldRejectWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var opponent = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/games",
            CreateRequest(context.Administrator.UserId, opponent.UserId, 12, 9),
            TestJsonSerializerOptions.Default
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    [Fact]
    public async Task RecordGame_WhenMembershipProviderFails_ShouldReturnProblemDetailsWithoutWritingGame()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var opponent = await CreateMemberAsync(
            fakeWorkOs,
            context.Crew.OrganizationId
        );
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
        Assert.Equal(0, await CountGamesAsync(context.Crew.CrewId));
    }

    private async Task<int> CountGamesAsync(Guid crewUuid)
    {
        await using var dbContext = fixture.CreateDbContext();

        return await dbContext.Games.CountAsync(game => game.Crew.Uuid == crewUuid);
    }

    private static async Task<TestIdentity> CreateMemberAsync(
        FakeWorkOsClient fakeWorkOs,
        string organizationId,
        string? displayName = null,
        string? profilePictureUrl = null,
        FakeMembershipStatus status = FakeMembershipStatus.Active)
    {
        var member = TestIdentity.Create() with { OrganizationId = organizationId };
        await fakeWorkOs.CreateUserAsync(
            member.UserId,
            member.EmailAddress,
            displayName: displayName,
            profilePictureUrl: profilePictureUrl
        );
        await fakeWorkOs.CreateMembershipAsync(
            $"membership_test_{Guid.NewGuid():N}",
            member.UserId,
            organizationId,
            FakeMembershipRole.Member,
            status
        );

        return member;
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

    private static void AssertMemberParticipant(
        GameParticipant participant,
        int slotNumber,
        string userId)
    {
        Assert.Equal(slotNumber, participant.SlotNumber);
        Assert.Equal(userId, participant.UserId);
        Assert.Null(participant.GuestName);
    }

    private static void AssertGuestParticipant(
        GameParticipant participant,
        int slotNumber,
        string guestName)
    {
        Assert.Equal(slotNumber, participant.SlotNumber);
        Assert.Null(participant.UserId);
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
