using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Statistics;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class RemoveCrewMemberEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task RemoveCrewMember_WhenAdministratorRemovesActiveMember_ShouldDeactivateMembershipAndPreserveStatistics()
    {
        var removedUserId = $"user_test_{Guid.NewGuid():N}";
        var opponentUserId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: removedUserId, displayName: "Amy Dink")
            .WithMember(id: opponentUserId, displayName: "Ben Volley")
            .WithSinglesGame(
                side1: (removedUserId, 11),
                side2: (opponentUserId, 5)
            )
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.DeleteAsync(
            $"/crews/members/{Uri.EscapeDataString(removedUserId)}"
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembers = await fakeWorkOs.ListActiveMembersAsync(
            context.OrganizationId
        );
        Assert.DoesNotContain(
            activeMembers,
            member => member.WorkOsUserId == removedUserId
        );

        await using var database = fixture.CreateDbContext();
        var statistics = await database.PlayerStatistics
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewId == context.Id
                && candidate.UserId == removedUserId
            );
        Assert.Equal(1, statistics.Wins);
        Assert.Equal(106, statistics.CrewScore);
    }

    [Fact]
    public async Task RemoveCrewMember_WhenStatisticsAreCached_ShouldInvalidateCrewAndPersonalStatistics()
    {
        var removedUserId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithMember(id: removedUserId)
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        var cachedLeaderboard = await GetLeaderboardAsync(apiClient);
        var cachedPersonalStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.Contains(
            cachedLeaderboard.Entries,
            entry => entry.UserId == removedUserId
        );
        Assert.Equal(2, cachedPersonalStatistics.ActiveCrewMemberCount);

        using var response = await apiClient.DeleteAsync(
            $"/crews/members/{Uri.EscapeDataString(removedUserId)}"
        );
        response.EnsureSuccessStatusCode();

        var refreshedLeaderboard = await GetLeaderboardAsync(apiClient);
        var refreshedPersonalStatistics = await GetPersonalStatisticsAsync(apiClient);
        Assert.DoesNotContain(
            refreshedLeaderboard.Entries,
            entry => entry.UserId == removedUserId
        );
        Assert.Equal(1, refreshedPersonalStatistics.ActiveCrewMemberCount);
    }

    [Fact]
    public async Task RemoveCrewMember_WhenAdministratorRemovesThemselves_ShouldReturnForbiddenAndKeepMembershipActive()
    {
        var context = await fixture.CreateCrewBuilder().BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.DeleteAsync(
            $"/crews/members/{Uri.EscapeDataString(context.Administrator.UserId)}"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            context.Administrator.UserId,
            context.OrganizationId
        );

        Assert.NotNull(activeMembershipId);
    }

    [Fact]
    public async Task RemoveCrewMember_WhenCallerIsNotAdministrator_ShouldReturnForbidden()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember()
            .WithMember()
            .BuildAsync();
        var caller = context.Members[0];
        var target = context.Members[1];
        using var apiClient = fixture.CreateApiClient(caller);

        using var response = await apiClient.DeleteAsync(
            $"/crews/members/{Uri.EscapeDataString(target.UserId)}"
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            target.UserId,
            context.OrganizationId
        );

        Assert.NotNull(activeMembershipId);
    }

    [Fact]
    public async Task RemoveCrewMember_WhenTargetIsNotAnActiveCrewMember_ShouldReturnNotFound()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(status: FakeMembershipStatus.Inactive)
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.DeleteAsync(
            $"/crews/members/{Uri.EscapeDataString(context.Members[0].UserId)}"
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<GetCrewLeaderboardHttpResponse> GetLeaderboardAsync(
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
