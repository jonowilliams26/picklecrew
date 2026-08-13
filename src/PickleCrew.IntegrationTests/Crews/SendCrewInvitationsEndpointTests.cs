using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Crews;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Crews;

[Collection(nameof(AppHostCollection))]
public sealed class SendCrewInvitationsEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task SendCrewInvitations_WhenAdministratorInvitesFriend_ShouldSendMemberInvitation()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        using var apiClient = fixture.CreateApiClient(activeCreator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            new SendCrewInvitationsHttpRequest
            {
                EmailAddresses = [" friend@example.com "],
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<SendCrewInvitationsHttpResponse>();
        Assert.NotNull(result);
        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal("friend@example.com", outcome.EmailAddress);
        Assert.Equal(CrewInvitationOutcomeStatus.Sent, outcome.Status);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var invitation = Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId)
        );

        Assert.Equal(crew.OrganizationId, invitation.OrganizationId);
        Assert.Equal("friend@example.com", invitation.EmailAddress);
        Assert.Equal(FakeInvitationState.Pending, invitation.State);
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
        Assert.Equal(creator.UserId, invitation.InviterUserId);
    }

    [Fact]
    public async Task SendCrewInvitations_WhenUserIsNotAdministrator_ShouldReturnForbidden()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "member",
        };
        using var apiClient = fixture.CreateApiClient(activeCreator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SendCrewInvitations_WhenAdministratorDidNotCreateCrew_ShouldSendMemberInvitation()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var otherAdministrator = TestIdentity.Create("admin") with
        {
            OrganizationId = crew.OrganizationId,
        };
        using var apiClient = fixture.CreateApiClient(otherAdministrator);

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        var invitation = Assert.Single(
            await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId)
        );
        Assert.Equal(otherAdministrator.UserId, invitation.InviterUserId);
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
    }

    [Fact]
    public async Task SendCrewInvitations_WhenActiveOrganizationIsNotCrew_ShouldReturnForbidden()
    {
        using var apiClient = fixture.CreateApiClient(TestIdentity.Create("admin"));

        using var response = await apiClient.PostAsJsonAsync(
            "/crews/invitations",
            CreateRequest()
        );

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<CreateCrewHttpResponse> CreateCrewAsync(TestIdentity identity)
    {
        using var apiClient = fixture.CreateApiClient(identity);
        using var response = await apiClient.PostAsJsonAsync(
            "/crews",
            new CreateCrewHttpRequest { Name = "Kitchen Legends" }
        );

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CreateCrewHttpResponse>()
            ?? throw new InvalidOperationException("The API returned an empty crew response.");
    }

    private static SendCrewInvitationsHttpRequest CreateRequest()
    {
        return new SendCrewInvitationsHttpRequest
        {
            EmailAddresses = ["friend@example.com"],
        };
    }
}
