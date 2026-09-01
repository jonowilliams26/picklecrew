using System.Net;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

[Collection(nameof(AppHostCollection))]
public sealed class FakeWorkOsEndpointsTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Setup_WhenRequestIsValid_ShouldCreateOrganizationUserAndMembership()
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        var setup = await client.SetupAsync(
            identity.UserId,
            identity.EmailAddress,
            "Kitchen Legends",
            externalId,
            FakeMembershipRole.Member
        );

        Assert.Equal(identity.UserId, setup.UserId);
        Assert.StartsWith("org_", setup.OrganizationId);
        Assert.StartsWith("om_", setup.MembershipId);
        Assert.Equal(
            setup.OrganizationId,
            await client.FindOrganizationByExternalIdAsync(externalId)
        );
        Assert.Equal(
            setup.MembershipId,
            await client.FindActiveMembershipAsync(identity.UserId, setup.OrganizationId)
        );
    }

    [Fact]
    public async Task CreateEndpoints_WhenMembershipIsActive_ShouldMakeStateFindable()
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";
        var membershipId = $"om_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        var createdUserId = await client.CreateUserAsync(
            identity.UserId,
            identity.EmailAddress
        );
        var createdOrganizationId = await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            externalId
        );
        var createdMembershipId = await client.CreateMembershipAsync(
            membershipId,
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        Assert.Equal(identity.UserId, createdUserId);
        Assert.Equal(identity.OrganizationId, createdOrganizationId);
        Assert.Equal(membershipId, createdMembershipId);
        Assert.Equal(
            identity.OrganizationId,
            await client.FindOrganizationByExternalIdAsync(externalId)
        );
        Assert.Equal(
            membershipId,
            await client.FindActiveMembershipAsync(
                identity.UserId,
                identity.OrganizationId!
            )
        );
    }

    [Fact]
    public async Task DeactivateMembership_WhenMembershipIsActive_ShouldMakeMembershipInactive()
    {
        var identity = TestIdentity.Create();
        var membershipId = $"om_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateUserAsync(identity.UserId, identity.EmailAddress);
        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.CreateMembershipAsync(
            membershipId,
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        await client.DeactivateMembershipAsync(membershipId);

        Assert.Null(await client.FindActiveMembershipAsync(
            identity.UserId,
            identity.OrganizationId!
        ));
    }

    [Fact]
    public async Task FindActiveMembership_WhenMembershipIsInactive_ShouldReturnNotFound()
    {
        var identity = TestIdentity.Create();
        var membershipId = $"om_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateUserAsync(
            identity.UserId,
            identity.EmailAddress
        );
        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.CreateMembershipAsync(
            membershipId,
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Inactive
        );

        var foundMembershipId = await client.FindActiveMembershipAsync(
            identity.UserId,
            identity.OrganizationId!
        );

        Assert.Null(foundMembershipId);
    }

    [Fact]
    public async Task ListActiveMembers_WhenUsersAndCrewsDiffer_ShouldReturnCrewScopedIdsAndDisplayNames()
    {
        var organizationId = $"org_test_{Guid.NewGuid():N}";
        var otherOrganizationId = $"org_test_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateOrganizationAsync(
            organizationId,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.CreateOrganizationAsync(
            otherOrganizationId,
            "Baseline Bandits",
            $"crew_test_{Guid.NewGuid():N}"
        );

        var preferredNameUserId = $"user_test_{Guid.NewGuid():N}";
        await client.CreateUserAsync(
            preferredNameUserId,
            "preferred@example.test",
            "Pat",
            "Lee",
            "Paddle Pat",
            "https://images.example.test/pat.png"
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            preferredNameUserId,
            organizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        var fullNameUserId = $"user_test_{Guid.NewGuid():N}";
        await client.CreateUserAsync(
            fullNameUserId,
            "full-name@example.test",
            "Morgan",
            "Reed"
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            fullNameUserId,
            organizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        var emailFallbackUserId = $"user_test_{Guid.NewGuid():N}";
        await client.CreateUserAsync(
            emailFallbackUserId,
            "fallback@example.test"
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            emailFallbackUserId,
            organizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        var inactiveUserId = $"user_test_{Guid.NewGuid():N}";
        await client.CreateUserAsync(
            inactiveUserId,
            "inactive@example.test",
            displayName: "Inactive Player"
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            inactiveUserId,
            organizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Inactive
        );

        var otherCrewUserId = $"user_test_{Guid.NewGuid():N}";
        await client.CreateUserAsync(
            otherCrewUserId,
            "other-crew@example.test",
            displayName: "Other Crew Player"
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            otherCrewUserId,
            otherOrganizationId,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        var members = await client.ListActiveMembersAsync(organizationId);

        Assert.Equal(3, members.Length);
        Assert.Contains(
            members,
            member => member.WorkOsUserId == preferredNameUserId
                && member.DisplayName == "Paddle Pat"
                && member.EmailAddress == "preferred@example.test"
                && member.ProfilePictureUrl == "https://images.example.test/pat.png"
        );
        Assert.Contains(
            members,
            member => member.WorkOsUserId == fullNameUserId
                && member.DisplayName == "Morgan Reed"
                && member.EmailAddress == "full-name@example.test"
        );
        Assert.Contains(
            members,
            member => member.WorkOsUserId == emailFallbackUserId
                && member.DisplayName == "fallback@example.test"
                && member.EmailAddress == "fallback@example.test"
                && member.ProfilePictureUrl is null
        );
        Assert.DoesNotContain(
            members,
            member => member.WorkOsUserId == inactiveUserId
                || member.WorkOsUserId == otherCrewUserId
        );
    }

    [Fact]
    public async Task InvitationEndpoints_WhenInvitationStatesDiffer_ShouldExposeEligibilityAndInvitationDetails()
    {
        var creator = TestIdentity.Create(FakeMembershipRole.Admin.ToString());
        var activeMember = TestIdentity.Create();
        var inactiveMember = TestIdentity.Create();
        var pendingEmailAddress = "pending@example.test";
        var acceptedEmailAddress = "accepted@example.test";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateOrganizationAsync(
            creator.OrganizationId!,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.CreateUserAsync(
            activeMember.UserId,
            activeMember.EmailAddress
        );
        await client.CreateUserAsync(
            inactiveMember.UserId,
            inactiveMember.EmailAddress
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            activeMember.UserId,
            creator.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            inactiveMember.UserId,
            creator.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Inactive
        );
        var pendingInvitation = await client.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            creator.OrganizationId!,
            pendingEmailAddress,
            FakeMembershipRole.Member,
            creator.UserId
        );
        await client.CreateInvitationAsync(
            $"invitation_test_{Guid.NewGuid():N}",
            creator.OrganizationId!,
            acceptedEmailAddress,
            FakeMembershipRole.Member,
            creator.UserId,
            FakeInvitationState.Accepted
        );

        var activeMembers = await client.ListActiveMembersAsync(
            creator.OrganizationId!
        );
        var pendingInvitations = await client.ListPendingInvitationsAsync(
            creator.OrganizationId!
        );
        var allInvitations = await client.ListInvitationsAsync(
            creator.OrganizationId!
        );

        var foundActiveMember = Assert.Single(activeMembers);
        Assert.Equal(activeMember.UserId, foundActiveMember.WorkOsUserId);
        Assert.Equal(activeMember.EmailAddress, foundActiveMember.DisplayName);
        Assert.Equal(activeMember.EmailAddress, foundActiveMember.EmailAddress);
        var foundPendingInvitation = Assert.Single(pendingInvitations);
        Assert.Equal(pendingInvitation, foundPendingInvitation);
        Assert.Equal(FakeInvitationState.Pending, foundPendingInvitation.State);
        Assert.Equal(FakeMembershipRole.Member, foundPendingInvitation.Role);
        Assert.Equal(creator.UserId, foundPendingInvitation.InviterUserId);
        Assert.Equal(2, allInvitations.Length);
        Assert.Contains(
            allInvitations,
            invitation => invitation.EmailAddress == acceptedEmailAddress
                && invitation.State == FakeInvitationState.Accepted
        );
    }

    [Fact]
    public async Task SendInvitation_WhenRecipientFaultExists_ShouldFailOnlyForRecipientAndScenario()
    {
        var identity = TestIdentity.Create(FakeMembershipRole.Admin.ToString());
        var failedScenarioId = Guid.NewGuid().ToString("N");
        var healthyScenarioId = Guid.NewGuid().ToString("N");
        var failedRecipient = "failed@example.test";
        var healthyRecipient = "healthy@example.test";
        using var client = fixture.CreateFakeWorkOsClient();

        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            $"crew_test_{Guid.NewGuid():N}"
        );
        await client.FailInvitationAsync(
            failedScenarioId,
            failedRecipient.ToUpperInvariant()
        );

        using var failedResponse = await client.SendInvitationAsync(
            identity.OrganizationId!,
            failedRecipient,
            FakeMembershipRole.Member,
            identity.UserId,
            failedScenarioId
        );
        using var otherRecipientResponse = await client.SendInvitationAsync(
            identity.OrganizationId!,
            healthyRecipient,
            FakeMembershipRole.Member,
            identity.UserId,
            failedScenarioId
        );
        using var otherScenarioResponse = await client.SendInvitationAsync(
            identity.OrganizationId!,
            failedRecipient,
            FakeMembershipRole.Member,
            identity.UserId,
            healthyScenarioId
        );

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedResponse.StatusCode);
        Assert.True(otherRecipientResponse.IsSuccessStatusCode);
        Assert.True(otherScenarioResponse.IsSuccessStatusCode);

        var invitations = await client.ListInvitationsAsync(identity.OrganizationId!);
        Assert.Equal(2, invitations.Length);
        Assert.All(
            invitations,
            invitation => Assert.Equal(FakeMembershipRole.Member, invitation.Role)
        );
    }

    [Theory]
    [InlineData(FakeWorkOsOperation.FindOrganizationByExternalId)]
    [InlineData(FakeWorkOsOperation.CreateOrganization)]
    [InlineData(FakeWorkOsOperation.FindActiveMembership)]
    [InlineData(FakeWorkOsOperation.CreateMembership)]
    public async Task ProvisioningOperation_WhenFaultExists_ShouldFailOnlyForScenario(
        FakeWorkOsOperation operation)
    {
        using var client = fixture.CreateFakeWorkOsClient();
        var failedScenario = await CreateScenarioAsync(client);
        var healthyScenario = await CreateScenarioAsync(client);
        var failedScenarioId = Guid.NewGuid().ToString("N");
        var healthyScenarioId = Guid.NewGuid().ToString("N");

        await client.FailOperationAsync(failedScenarioId, operation);

        using var failedResponse = await SendOperationAsync(
            client,
            operation,
            failedScenario,
            failedScenarioId
        );
        using var healthyResponse = await SendOperationAsync(
            client,
            operation,
            healthyScenario,
            healthyScenarioId
        );

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedResponse.StatusCode);
        Assert.True(healthyResponse.IsSuccessStatusCode);
    }

    private static async Task<FakeWorkOsScenario> CreateScenarioAsync(
        FakeWorkOsClient client)
    {
        var identity = TestIdentity.Create();
        var externalId = $"crew_test_{Guid.NewGuid():N}";

        await client.CreateUserAsync(
            identity.UserId,
            identity.EmailAddress
        );
        await client.CreateOrganizationAsync(
            identity.OrganizationId!,
            "Kitchen Legends",
            externalId
        );
        await client.CreateMembershipAsync(
            $"om_test_{Guid.NewGuid():N}",
            identity.UserId,
            identity.OrganizationId!,
            FakeMembershipRole.Member,
            FakeMembershipStatus.Active
        );

        return new FakeWorkOsScenario
        {
            UserId = identity.UserId,
            OrganizationId = identity.OrganizationId!,
            OrganizationExternalId = externalId,
        };
    }

    private static Task<HttpResponseMessage> SendOperationAsync(
        FakeWorkOsClient client,
        FakeWorkOsOperation operation,
        FakeWorkOsScenario scenario,
        string scenarioId)
    {
        return operation switch
        {
            FakeWorkOsOperation.FindOrganizationByExternalId =>
                client.GetOrganizationByExternalIdAsync(
                    scenario.OrganizationExternalId,
                    scenarioId
                ),
            FakeWorkOsOperation.CreateOrganization => client.SendCreateOrganizationAsync(
                "Kitchen Legends",
                scenario.OrganizationExternalId,
                scenarioId
            ),
            FakeWorkOsOperation.FindActiveMembership => client.GetActiveMembershipAsync(
                scenario.UserId,
                scenario.OrganizationId,
                scenarioId
            ),
            FakeWorkOsOperation.CreateMembership => client.SendCreateMembershipAsync(
                scenario.UserId,
                scenario.OrganizationId,
                FakeMembershipRole.Member,
                scenarioId
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };
    }

    private sealed record FakeWorkOsScenario
    {
        public required string UserId { get; init; }

        public required string OrganizationId { get; init; }

        public required string OrganizationExternalId { get; init; }
    }
}
