using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class LeaveCrewEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task LeaveCrew_WhenMemberConfirmsCrewName_ShouldDeactivateOwnMembershipAndPreserveStatistics()
    {
        var memberId = $"user_test_{Guid.NewGuid():N}";
        var administratorId = $"user_test_{Guid.NewGuid():N}";
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithAdministrator(id: administratorId)
            .WithMember(id: memberId)
            .WithSinglesGame(
                side1: (memberId, 11),
                side2: (administratorId, 5)
            )
            .BuildAsync();
        var member = Assert.Single(context.Members);
        using var apiClient = fixture.CreateApiClient(member);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/leave",
            new LeaveCrewHttpRequest
            {
                CrewName = "Kitchen Legends",
            }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            memberId,
            context.OrganizationId
        );
        Assert.Null(activeMembershipId);

        await using var database = fixture.CreateDbContext();
        var statistics = await database.PlayerStatistics
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.CrewId == context.Id
                && candidate.UserId == memberId
            );
        Assert.Equal(1, statistics.Wins);
        Assert.Equal(106, statistics.CrewScore);
    }

    [Fact]
    public async Task LeaveCrew_WhenAnotherAdministratorIsActive_ShouldDeactivateOwnMembership()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithMember(role: FakeMembershipRole.Admin)
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var eligibilityResponse = await apiClient.GetAsync(
            "/crews/leave-eligibility"
        );
        Assert.Equal(HttpStatusCode.OK, eligibilityResponse.StatusCode);
        var eligibility = await eligibilityResponse.Content
            .ReadFromJsonAsync<GetCrewLeavingEligibilityHttpResponse>();
        Assert.NotNull(eligibility);
        Assert.True(eligibility.CanLeave);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/leave",
            new LeaveCrewHttpRequest
            {
                CrewName = "Kitchen Legends",
            }
        );

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            context.Administrator.UserId,
            context.OrganizationId
        );
        Assert.Null(activeMembershipId);
    }

    [Fact]
    public async Task LeaveCrew_WhenCurrentUserIsSoleAdministrator_ShouldReturnConflictAndKeepMembershipActive()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithMember()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var eligibilityResponse = await apiClient.GetAsync(
            "/crews/leave-eligibility"
        );
        Assert.Equal(HttpStatusCode.OK, eligibilityResponse.StatusCode);
        var eligibility = await eligibilityResponse.Content
            .ReadFromJsonAsync<GetCrewLeavingEligibilityHttpResponse>();
        Assert.NotNull(eligibility);
        Assert.False(eligibility.CanLeave);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/leave",
            new LeaveCrewHttpRequest
            {
                CrewName = "Kitchen Legends",
            }
        );

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            context.Administrator.UserId,
            context.OrganizationId
        );
        Assert.NotNull(activeMembershipId);
    }

    [Fact]
    public async Task LeaveCrew_WhenCrewNameDoesNotMatchExactly_ShouldReturnBadRequestAndKeepMembershipActive()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithName("Kitchen Legends")
            .WithMember()
            .BuildAsync();
        var member = Assert.Single(context.Members);
        using var apiClient = fixture.CreateApiClient(member);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/leave",
            new LeaveCrewHttpRequest
            {
                CrewName = "kitchen legends",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var activeMembershipId = await fakeWorkOs.FindActiveMembershipAsync(
            member.UserId,
            context.OrganizationId
        );
        Assert.NotNull(activeMembershipId);
    }
}
