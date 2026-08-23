using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class SendCrewInvitationEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task SendCrewInvitation_WhenAdministratorInvitesFriend_ShouldSendMemberInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = " friend@example.com ",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(result);
        AssertOutcome(
            result,
            "friend@example.com",
            CrewInvitationOutcomeStatus.Sent
        );

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var invitation = Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );

        Assert.Equal(context.OrganizationId, invitation.OrganizationId);
        Assert.Equal("friend@example.com", invitation.EmailAddress);
        Assert.Equal(FakeInvitationState.Pending, invitation.State);
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
        Assert.Equal(context.Administrator.UserId, invitation.InviterUserId);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenRecipientIsActiveMember_ShouldNotSendInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(emailAddress: "active@example.com")
            .BuildAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = " Active@Example.com ",
            }
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(result);
        AssertOutcome(
            result,
            "Active@Example.com",
            CrewInvitationOutcomeStatus.AlreadyActiveMember
        );
        Assert.Empty(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );
    }

    [Fact]
    public async Task SendCrewInvitation_WhenRecipientHasPendingInvitation_ShouldNotSendAnotherInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            context.OrganizationId,
            "PENDING@example.com",
            FakeMembershipRole.Member,
            context.Administrator.UserId
        );
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = "pending@example.com",
            }
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(result);
        AssertOutcome(
            result,
            "pending@example.com",
            CrewInvitationOutcomeStatus.AlreadyPending
        );
        Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );
    }

    [Fact]
    public async Task SendCrewInvitation_WhenExistingUserIsInactive_ShouldSendInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(
                emailAddress: "inactive@example.com",
                status: FakeMembershipStatus.Inactive
            )
            .BuildAsync();
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        using var apiClient = fixture.CreateApiClient(context.Administrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = "inactive@example.com",
            }
        );

        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(result);
        AssertOutcome(
            result,
            "inactive@example.com",
            CrewInvitationOutcomeStatus.Sent
        );

        var invitation = Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenSendFails_ShouldReturnFailureAndAllowRetry()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailInvitationAsync(scenarioId, "retry@example.com");
        using var failingApiClient = fixture.CreateApiClient(
            context.Administrator,
            scenarioId
        );

        using var failedResponse = await failingApiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = "retry@example.com",
            }
        );

        failedResponse.EnsureSuccessStatusCode();
        var failedResult = await failedResponse.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(failedResult);
        AssertOutcome(
            failedResult,
            "retry@example.com",
            CrewInvitationOutcomeStatus.Failed
        );
        Assert.Empty(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );

        using var retryingApiClient = fixture.CreateApiClient(context.Administrator);
        using var retryResponse = await retryingApiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationHttpRequest
            {
                EmailAddress = "retry@example.com",
            }
        );

        retryResponse.EnsureSuccessStatusCode();
        var retryResult = await retryResponse.Content
            .ReadFromJsonAsync<SendCrewInvitationHttpResponse>(
                TestJsonSerializerOptions.Default
            );
        Assert.NotNull(retryResult);
        AssertOutcome(
            retryResult,
            "retry@example.com",
            CrewInvitationOutcomeStatus.Sent
        );
        Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );
    }

    [Fact]
    public async Task SendCrewInvitation_WhenUserIsNotAdministrator_ShouldReturnForbidden()
    {
        var context = await fixture.CreateCrewBuilder()
            .BuildAsync();
        var member = context.Administrator with { Role = "member" };
        using var apiClient = fixture.CreateApiClient(member);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenAdministratorDidNotCreateCrew_ShouldSendMemberInvitation()
    {
        var context = await fixture.CreateCrewBuilder()
            .WithMember(role: FakeMembershipRole.Admin)
            .BuildAsync();
        var otherAdministrator = context.Members[0];
        using var apiClient = fixture.CreateApiClient(otherAdministrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var invitation = Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(context.OrganizationId)
        );
        Assert.Equal(otherAdministrator.UserId, invitation.InviterUserId);
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenActiveOrganizationIsNotCrew_ShouldReturnForbidden()
    {
        using var apiClient = fixture.CreateApiClient(TestIdentity.Create("admin"));

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static void AssertOutcome(
        SendCrewInvitationHttpResponse outcome,
        string emailAddress,
        CrewInvitationOutcomeStatus status)
    {
        Assert.Equal(emailAddress, outcome.EmailAddress);
        Assert.Equal(status, outcome.Status);
    }

    private static SendCrewInvitationHttpRequest CreateRequest()
    {
        return new SendCrewInvitationHttpRequest
        {
            EmailAddress = "friend@example.com",
        };
    }
}
