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
            await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId)
        );

        Assert.Equal(crew.OrganizationId, invitation.OrganizationId);
        Assert.Equal("friend@example.com", invitation.EmailAddress);
        Assert.Equal(FakeInvitationState.Pending, invitation.State);
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
        Assert.Equal(creator.UserId, invitation.InviterUserId);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenRecipientIsActiveMember_ShouldNotSendInvitation()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await CreateMemberAsync(
            fakeWorkOs,
            crew.OrganizationId,
            "active@example.com",
            FakeMembershipStatus.Active
        );
        using var apiClient = fixture.CreateApiClient(activeCreator);

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
        Assert.Empty(await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId));
    }

    [Fact]
    public async Task SendCrewInvitation_WhenRecipientHasPendingInvitation_ShouldNotSendAnotherInvitation()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            crew.OrganizationId,
            "PENDING@example.com",
            FakeMembershipRole.Member,
            creator.UserId
        );
        using var apiClient = fixture.CreateApiClient(activeCreator);

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
        Assert.Single(await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId));
    }

    [Fact]
    public async Task SendCrewInvitation_WhenExistingUserIsInactive_ShouldSendInvitation()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await CreateMemberAsync(
            fakeWorkOs,
            crew.OrganizationId,
            "inactive@example.com",
            FakeMembershipStatus.Inactive
        );
        using var apiClient = fixture.CreateApiClient(activeCreator);

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
            await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId)
        );
        Assert.Equal(FakeMembershipRole.Member, invitation.Role);
    }

    [Fact]
    public async Task SendCrewInvitation_WhenSendFails_ShouldReturnFailureAndAllowRetry()
    {
        var creator = TestIdentity.CreateSubjectOnly();
        var crew = await CreateCrewAsync(creator);
        var activeCreator = creator with
        {
            OrganizationId = crew.OrganizationId,
            Role = "admin",
        };
        var scenarioId = Guid.NewGuid().ToString("N");
        using var fakeWorkOs = fixture.CreateFakeWorkOsClient();
        await fakeWorkOs.FailInvitationAsync(scenarioId, "retry@example.com");
        using var failingApiClient = fixture.CreateApiClient(activeCreator, scenarioId);

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
        Assert.Empty(await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId));

        using var retryingApiClient = fixture.CreateApiClient(activeCreator);
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
        Assert.Single(await fakeWorkOs.ListInvitationsAsync(crew.OrganizationId));
    }

    [Fact]
    public async Task SendCrewInvitation_WhenUserIsNotAdministrator_ShouldReturnForbidden()
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
    public async Task SendCrewInvitation_WhenAdministratorDidNotCreateCrew_ShouldSendMemberInvitation()
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
    public async Task SendCrewInvitation_WhenActiveOrganizationIsNotCrew_ShouldReturnForbidden()
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
            ?? throw new InvalidOperationException(
                "The API returned an empty crew response."
            );
    }

    private static async Task CreateMemberAsync(
        FakeWorkOsClient fakeWorkOs,
        string organizationId,
        string emailAddress,
        FakeMembershipStatus status)
    {
        var userId = $"user_test_{Guid.NewGuid():N}";
        await fakeWorkOs.CreateUserAsync(userId, emailAddress);
        await fakeWorkOs.CreateMembershipAsync(
            $"membership_test_{Guid.NewGuid():N}",
            userId,
            organizationId,
            FakeMembershipRole.Member,
            status
        );
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
