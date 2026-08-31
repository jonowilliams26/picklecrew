using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games;
using PickleCrew.Api.Games.RecordGame;
using PickleCrew.Api.Shared;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Games;

[Collection(nameof(AppHostCollection))]
public sealed class GetGamesEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GetGames_WhenCrewHasMoreThanOnePage_ShouldReturnNewestPageWithinCrew()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var crewBuilder = fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId);

        for (var index = 0; index < 11; index++)
        {
            crewBuilder.WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Guest Player", 7)
            );
        }

        var context = await crewBuilder.BuildAsync();
        var otherAdministratorId = $"user_test_{Guid.NewGuid():N}";
        await fixture.CreateCrewBuilder()
            .WithAdministrator(id: otherAdministratorId)
            .WithSinglesGame(
                side1: (otherAdministratorId, 11),
                side2: ("Other Guest", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var tenItemPageResponse = await apiClient.GetAsync(
            "/games?page=1&pageSize=10"
        );
        tenItemPageResponse.EnsureSuccessStatusCode();

        using var firstPageResponse = await apiClient.GetAsync(
            "/games?page=1&pageSize=5"
        );
        firstPageResponse.EnsureSuccessStatusCode();
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(firstPage);
        Assert.Equal(5, firstPage.Items.Length);

        using var response = await apiClient.GetAsync("/games?page=3&pageSize=5");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        Assert.Equal(3, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(11, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        var game = Assert.Single(result.Items);
        Assert.Equal(context.Games[0].PlayedAtUtc, game.PlayedAtUtc);
        Assert.Equal(
            context.Administrator.EmailAddress,
            game.Sides[0].Participants[0].DisplayName
        );
    }

    [Fact]
    public async Task GetGames_WhenGameIncludesMembersAndGuests_ShouldReturnCompleteResult()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId, displayName: "Taylor Lob")
            .WithDoublesGame(
                side1: (administratorId, "Jamie Kitchen", 13),
                side2: (memberId, "Casey Court", 11)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var returnedGame = Assert.Single(result.Items);
        Assert.Equal(GameFormat.Doubles, returnedGame.Format);
        Assert.Equal(13, returnedGame.Sides[0].Score);
        Assert.Equal(11, returnedGame.Sides[1].Score);
        Assert.Collection(
            returnedGame.Sides[0].Participants,
            participant =>
            {
                Assert.Equal(
                    context.Administrator.EmailAddress,
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
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        const string profilePictureUrl = "https://example.test/player.jpg";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(
                id: administratorId,
                displayName: "Morgan Dink",
                profilePictureUrl: profilePictureUrl
            )
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Guest Player", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.GetAsync("/games");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(result);
        var game = Assert.Single(result.Items);
        var participant = Assert.Single(game.Sides[0].Participants);
        Assert.Equal("Morgan Dink", participant.DisplayName);
        Assert.Equal(profilePictureUrl, participant.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetGames_WhenPersistedHistoryChangesWithoutInvalidation_ShouldReturnCachedHistory()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("Guest Player", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var initialResponse = await apiClient.GetAsync("/games");
        initialResponse.EnsureSuccessStatusCode();
        var initialHistory = await initialResponse.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(initialHistory);
        Assert.Equal(7, Assert.Single(initialHistory.Items).Sides[1].Score);

        var distributedCacheOptions = Options.Create(new RedisCacheOptions
        {
            Configuration = fixture.CacheConnectionString,
            InstanceName = "picklecrew:",
        });

        using (var distributedCache = new RedisCache(distributedCacheOptions))
        {
            var cachedValue = await distributedCache.GetAsync(
                $"v2:{new GamesHistoryCacheOption(context.Uuid, 1, 10).Key}"
            );
            Assert.NotNull(cachedValue);
            Assert.NotEmpty(cachedValue);
        }

        var uncachedGameId = Guid.NewGuid();
        await using (var database = fixture.CreateDbContext())
        {
            database.Games.Add(new Game
            {
                Uuid = uncachedGameId,
                CrewId = context.Id,
                CrewUuid = context.Uuid,
                Format = GameFormat.Singles,
                RecorderUserId = administratorId,
                PlayedAtUtc = context.Games[0].PlayedAtUtc.AddMinutes(1),
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
                                UserId = administratorId,
                                MemberDisplayName =
                                    context.Administrator.EmailAddress,
                            },
                        ],
                    },
                    new GameSide
                    {
                        SideNumber = 2,
                        Score = 8,
                        Participants =
                        [
                            new GameParticipant
                            {
                                SlotNumber = 1,
                                GuestName = "Second Guest",
                            },
                        ],
                    },
                ],
            });
            await database.SaveChangesAsync();
        }

        PaginatedHttpResponse<GameHistoryItemHttpResponse>? cachedHistory;

        try
        {
            using var cachedResponse = await apiClient.GetAsync("/games");
            cachedResponse.EnsureSuccessStatusCode();
            cachedHistory = await cachedResponse.Content.ReadFromJsonAsync<
                PaginatedHttpResponse<GameHistoryItemHttpResponse>
            >(
                TestJsonSerializerOptions.Default
            );
        }
        finally
        {
            await using var database = fixture.CreateDbContext();
            await database.Games
                .Where(game => game.Uuid == uncachedGameId)
                .ExecuteDeleteAsync();
        }

        Assert.NotNull(cachedHistory);
        Assert.Equal(1, cachedHistory.TotalCount);
        Assert.Equal(7, Assert.Single(cachedHistory.Items).Sides[1].Score);
    }

    [Fact]
    public async Task GetGames_WhenGameIsRecordedAfterHistoryIsCached_ShouldReturnInvalidatedHistory()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .WithSinglesGame(
                side1: (administratorId, 11),
                side2: ("First Guest", 7)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var cachedResponse = await apiClient.GetAsync("/games");
        cachedResponse.EnsureSuccessStatusCode();
        var cachedHistory = await cachedResponse.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(cachedHistory);
        Assert.Single(cachedHistory.Items);

        using var recordResponse = await apiClient.PostAsJsonAsync(
            "/games",
            new RecordGameHttpRequest
            {
                Format = GameFormat.Singles,
                Sides =
                [
                    new RecordGameSideHttpRequest
                    {
                        Score = 11,
                        Participants =
                        [
                            new RecordGameParticipantHttpRequest
                            {
                                UserId = administratorId,
                            },
                        ],
                    },
                    new RecordGameSideHttpRequest
                    {
                        Score = 8,
                        Participants =
                        [
                            new RecordGameParticipantHttpRequest
                            {
                                GuestName = "Second Guest",
                            },
                        ],
                    },
                ],
            },
            TestJsonSerializerOptions.Default
        );
        recordResponse.EnsureSuccessStatusCode();
        var recordedGame = await recordResponse.Content.ReadFromJsonAsync<RecordGameHttpResponse>(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(recordedGame);

        using var refreshedResponse = await apiClient.GetAsync("/games");
        refreshedResponse.EnsureSuccessStatusCode();
        var refreshedHistory = await refreshedResponse.Content.ReadFromJsonAsync<
            PaginatedHttpResponse<GameHistoryItemHttpResponse>
        >(
            TestJsonSerializerOptions.Default
        );
        Assert.NotNull(refreshedHistory);
        Assert.Equal(2, refreshedHistory.TotalCount);
        Assert.Contains(
            refreshedHistory.Items,
            game => game.GameId == recordedGame.GameId
        );
    }

    [Fact]
    public async Task GetGames_WhenPaginationIsInvalid_ShouldReturnValidationProblem()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
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
}
