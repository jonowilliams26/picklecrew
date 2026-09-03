using System.Net.Http.Json;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
using PickleCrew.Api.Caching;
using PickleCrew.Api.Data;
using PickleCrew.Api.Games.RecordGame;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Statistics;

[Collection(nameof(AppHostCollection))]
public sealed class StatisticsCachingTests(AppHostFixture fixture)
{
    [Fact]
    public async Task StatisticsEndpoints_WhenRequested_ShouldCacheResponsesAndCrewStandings()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var crewId = context.Uuid;
        var userId = context.Administrator.UserId;
        var endpoints = new (string Path, ICacheOption CacheOption)[]
        {
            (
                "/statistics/best-doubles-pairing",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.BestDoublesPairing
                )
            ),
            (
                "/statistics/best-partner",
                new PersonalStatisticsCacheOption(
                    crewId,
                    userId,
                    PersonalStatisticsCacheResource.BestPartner
                )
            ),
            (
                "/statistics/biggest-rivals",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.BiggestRivals
                )
            ),
            (
                "/statistics/brick-wall",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.BrickWall
                )
            ),
            (
                "/statistics/chief-paddler",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.ChiefPaddler
                )
            ),
            (
                "/statistics/easy-win",
                new PersonalStatisticsCacheOption(
                    crewId,
                    userId,
                    PersonalStatisticsCacheResource.EasyWin
                )
            ),
            (
                "/statistics/hot-paddle",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.HotPaddle
                )
            ),
            (
                "/statistics/ice-man",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.IceMan
                )
            ),
            (
                "/statistics/leaderboard",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.CrewLeaderboard
                )
            ),
            (
                "/statistics/longest-win-streak",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.CrewLongestWinStreak
                )
            ),
            (
                "/statistics/friends-or-lovers",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.FriendsOrLovers
                )
            ),
            (
                "/statistics/limp-wrist-syndrome",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.LimpWristSyndrome
                )
            ),
            (
                "/statistics/point-machine",
                new CrewStatisticsCacheOption(
                    crewId,
                    CrewStatisticsCacheResource.PointMachine
                )
            ),
            (
                "/statistics/nemesis",
                new PersonalStatisticsCacheOption(
                    crewId,
                    userId,
                    PersonalStatisticsCacheResource.Nemesis
                )
            ),
            (
                "/statistics/personal",
                new PersonalStatisticsCacheOption(
                    crewId,
                    userId,
                    PersonalStatisticsCacheResource.PersonalStatistics
                )
            ),
            (
                "/statistics/recent-games",
                new PersonalStatisticsCacheOption(
                    crewId,
                    userId,
                    PersonalStatisticsCacheResource.RecentGames
                )
            ),
        };

        foreach (var endpoint in endpoints)
        {
            using var response = await apiClient.GetAsync(endpoint.Path);
            response.EnsureSuccessStatusCode();
        }

        var distributedCacheOptions = Options.Create(new RedisCacheOptions
        {
            Configuration = fixture.CacheConnectionString,
            InstanceName = "picklecrew:",
        });
        using var distributedCache = new RedisCache(distributedCacheOptions);

        foreach (var endpoint in endpoints)
        {
            var cachedValue = await distributedCache.GetAsync(
                $"v2:{endpoint.CacheOption.Key}"
            );

            Assert.NotNull(cachedValue);
            Assert.NotEmpty(cachedValue);
        }

        var standingsCacheOption = new CrewStatisticsCacheOption(
            crewId,
            CrewStatisticsCacheResource.CrewStandings
        );
        var cachedStandings = await distributedCache.GetAsync(
            $"v2:{standingsCacheOption.Key}"
        );

        Assert.NotNull(cachedStandings);
        Assert.NotEmpty(cachedStandings);
    }

    [Fact]
    public async Task PersonalStatistics_WhenMemberJoinsAfterStandingsAreCached_ShouldRefreshCrewStatistics()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .BuildAsync();
        using var administratorClient = fixture.CreateApiClient(context.Administrator);

        var initialLeaderboard = await GetCrewLeaderboardAsync(administratorClient);
        Assert.Single(initialLeaderboard.Entries);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.CreateUserAsync(
            memberId,
            $"{memberId}@example.test",
            displayName: "New Member"
        );
        await fakeWorkOs.CreateMembershipAsync(
            $"membership_test_{Guid.NewGuid():N}",
            memberId,
            context.OrganizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );
        var member = new TestIdentity
        {
            UserId = memberId,
            EmailAddress = $"{memberId}@example.test",
            OrganizationId = context.OrganizationId,
            Role = "member",
        };
        using var memberClient = fixture.CreateApiClient(member);

        var personalStatistics = await GetPersonalStatisticsAsync(memberClient);
        var refreshedLeaderboard = await GetCrewLeaderboardAsync(administratorClient);

        Assert.Equal(0, personalStatistics.GamesPlayed);
        Assert.Equal(2, personalStatistics.ActiveCrewMemberCount);
        Assert.Contains(
            refreshedLeaderboard.Entries,
            entry => string.Equals(entry.UserId, memberId, StringComparison.Ordinal)
        );
    }

    [Fact]
    public async Task PersonalStatistics_WhenGameIsRecordedAfterResponseIsCached_ShouldReturnInvalidatedResponse()
    {
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithAdministrator(id: administratorId)
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var initialStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Equal(0, initialStatistics.GamesPlayed);

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
                        Score = 7,
                        Participants =
                        [
                            new RecordGameParticipantHttpRequest
                            {
                                GuestName = "Guest Player",
                            },
                        ],
                    },
                ],
            },
            TestJsonSerializerOptions.Default
        );
        recordResponse.EnsureSuccessStatusCode();

        var refreshedStatistics = await GetPersonalStatisticsAsync(apiClient);

        Assert.Equal(1, refreshedStatistics.Wins);
        Assert.Equal(1, refreshedStatistics.GamesPlayed);
    }

    private static async Task<GetCrewLeaderboardHttpResponse> GetCrewLeaderboardAsync(
        HttpClient apiClient)
    {
        using var response = await apiClient.GetAsync("/statistics/leaderboard");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GetCrewLeaderboardHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException(
            "The API returned an empty crew leaderboard response."
        );
    }

    private static async Task<GetPersonalStatisticsHttpResponse> GetPersonalStatisticsAsync(
        HttpClient apiClient)
    {
        using var response = await apiClient.GetAsync("/statistics/personal");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<GetPersonalStatisticsHttpResponse>(
            TestJsonSerializerOptions.Default
        ) ?? throw new InvalidOperationException(
            "The API returned an empty personal statistics response."
        );
    }
}
