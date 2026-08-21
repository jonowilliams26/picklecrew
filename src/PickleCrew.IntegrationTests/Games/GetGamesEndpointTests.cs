using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class GetGamesEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGames_WhenCrewHasMoreThanOnePage_ShouldReturnNewestPageWithinCrew()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        var otherCrew = await fixture.CreateCrewWithAdministratorAsync();
        var crewId = await GetCrewDatabaseIdAsync(context);
        var otherCrewId = await GetCrewDatabaseIdAsync(otherCrew);
        var playedAt = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

        await AddGamesAsync(
            Enumerable.Range(0, 11)
                .Select(index => CreateGame(
                    context,
                    crewId,
                    playedAt.AddMinutes(index)
                ))
                .ToArray()
        );
        await AddGamesAsync(
            CreateGame(otherCrew, otherCrewId, playedAt.AddDays(1))
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games?page=3&pageSize=5");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        var game = Assert.Single(result.Games);
        Assert.Equal(playedAt, game.PlayedAtUtc);
        Assert.Equal(
            $"{context.Administrator.UserId}@picklecrew.test",
            game.Sides[0].Participants[0].DisplayName
        );
    }

    [Fact]
    public async Task GetGames_WhenGameIncludesMembersAndGuests_ShouldReturnCompleteResult()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        var crewId = await GetCrewDatabaseIdAsync(context);
        var game = CreateGame(
            context,
            crewId,
            new DateTimeOffset(2026, 8, 24, 18, 30, 0, TimeSpan.Zero)
        );
        game = new Game
        {
            CrewId = game.CrewId,
            CrewUuid = game.CrewUuid,
            RecorderUserId = game.RecorderUserId,
            Format = GameFormat.Doubles,
            PlayedAtUtc = game.PlayedAtUtc,
            Sides = [
                new GameSide
                {
                    SideNumber = 1,
                    Score = 13,
                    Participants = [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = context.Administrator.UserId,
                            MemberDisplayName = "Previous Morgan Dink",
                        },
                        new GameParticipant
                        {
                            SlotNumber = 2,
                            GuestName = "Jamie Kitchen",
                        },
                    ],
                },
                new GameSide
                {
                    SideNumber = 2,
                    Score = 11,
                    Participants = [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = "user_taylor",
                            MemberDisplayName = "Taylor Lob",
                        },
                        new GameParticipant
                        {
                            SlotNumber = 2,
                            GuestName = "Casey Court",
                        },
                    ],
                },
            ],
        };
        await AddGamesAsync(game);
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var returnedGame = Assert.Single(result.Games);
        Assert.Equal(GameFormat.Doubles, returnedGame.Format);
        Assert.Equal(13, returnedGame.Sides[0].Score);
        Assert.Equal(11, returnedGame.Sides[1].Score);
        Assert.Collection(
            returnedGame.Sides[0].Participants,
            participant =>
            {
                Assert.Equal(
                    $"{context.Administrator.UserId}@picklecrew.test",
                    participant.DisplayName
                );
                Assert.False(participant.IsGuest);
            },
            participant =>
            {
                Assert.Equal("Jamie Kitchen", participant.DisplayName);
                Assert.True(participant.IsGuest);
            }
        );
        Assert.Collection(
            returnedGame.Sides[1].Participants,
            participant =>
            {
                Assert.Equal("Taylor Lob", participant.DisplayName);
                Assert.False(participant.IsGuest);
            },
            participant =>
            {
                Assert.Equal("Casey Court", participant.DisplayName);
                Assert.True(participant.IsGuest);
            }
        );
    }

    [Fact]
    public async Task GetGames_WhenMemberHasProfilePicture_ShouldReturnProfilePicture()
    {
        var administrator = TestIdentity.CreateSubjectOnly();
        const string profilePictureUrl = "https://example.test/player.jpg";
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.CreateUserAsync(
            administrator.UserId,
            administrator.EmailAddress,
            displayName: "Morgan Dink",
            profilePictureUrl: profilePictureUrl
        );
        var crew = await fixture.CreateCrewAsync(administrator);
        var context = new CreatedCrewContext
        {
            Crew = crew,
            Administrator = administrator with
            {
                OrganizationId = crew.OrganizationId,
                Role = "admin",
            },
        };
        var crewId = await GetCrewDatabaseIdAsync(context);
        await AddGamesAsync(CreateGame(context, crewId, DateTimeOffset.UtcNow));
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GetGamesHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var game = Assert.Single(result.Games);
        var participant = Assert.Single(game.Sides[0].Participants);
        Assert.Equal("Morgan Dink", participant.DisplayName);
        Assert.Equal(profilePictureUrl, participant.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetGames_WhenPaginationIsInvalid_ShouldReturnValidationProblem()
    {
        var context = await fixture.CreateCrewWithAdministratorAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games?page=0&pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Page must be at least 1", content, StringComparison.Ordinal);
        Assert.Contains(
            "Page size must be between 1 and 100",
            content,
            StringComparison.Ordinal
        );
    }

    private async Task AddGamesAsync(params Game[] games)
    {
        await using var dbContext = fixture.CreateDbContext();
        dbContext.Games.AddRange(games);
        await dbContext.SaveChangesAsync();
    }

    private async Task<long> GetCrewDatabaseIdAsync(CreatedCrewContext context)
    {
        await using var dbContext = fixture.CreateDbContext();

        return await dbContext.Crews
            .Where(candidate => candidate.Uuid == context.Crew.CrewId)
            .Select(candidate => candidate.Id)
            .SingleAsync();
    }

    private static Game CreateGame(
        CreatedCrewContext context,
        long crewId,
        DateTimeOffset playedAt)
    {
        return new Game
        {
            CrewId = crewId,
            CrewUuid = context.Crew.CrewId,
            RecorderUserId = context.Administrator.UserId,
            Format = GameFormat.Singles,
            PlayedAtUtc = playedAt,
            Sides = [
                new GameSide
                {
                    SideNumber = 1,
                    Score = 11,
                    Participants = [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            UserId = context.Administrator.UserId,
                        },
                    ],
                },
                new GameSide
                {
                    SideNumber = 2,
                    Score = 7,
                    Participants = [
                        new GameParticipant
                        {
                            SlotNumber = 1,
                            GuestName = "Guest Player",
                        },
                    ],
                },
            ],
        };
    }
}
