using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using Xunit;

namespace PickleCrew.UnitTests.WorkOs;

public sealed class WorkOsCrewServiceTests
{
    [Fact]
    public async Task ListActiveCrewMembersAsync_WhenCalled_ShouldUseActiveCrewOrganization()
    {
        var workOs = new RecordingWorkOsService();
        var service = CreateService(workOs);

        await service.ListActiveCrewMembersAsync(CancellationToken.None);

        Assert.Equal("org_active", workOs.ActiveMembersOrganizationId);
    }

    [Fact]
    public async Task GetActiveMembersByIdAsync_WhenCalled_ShouldReturnMembersKeyedByUserId()
    {
        var member = new ActiveCrewMember
        {
            WorkOsUserId = "user_one",
            DisplayName = "User One",
            EmailAddress = "user.one@example.com",
            RoleSlug = "member",
        };
        var workOs = new RecordingWorkOsService
        {
            ActiveMembers = [member],
        };
        var service = CreateService(workOs);

        var membersById = await service.GetActiveMembersByIdAsync(
            CancellationToken.None
        );

        Assert.Same(member, membersById["user_one"]);
        Assert.False(membersById.ContainsKey("USER_ONE"));
        Assert.Equal("org_active", workOs.ActiveMembersOrganizationId);
    }

    [Fact]
    public async Task ListCrewMembersAsync_WhenCalled_ShouldUseActiveCrewOrganization()
    {
        var workOs = new RecordingWorkOsService();
        var service = CreateService(workOs);

        await service.ListCrewMembersAsync(CancellationToken.None);

        Assert.Equal("org_active", workOs.CrewMembersOrganizationId);
    }

    [Fact]
    public async Task SendCrewInvitationAsync_WhenCalled_ShouldUseActiveCrewOrganizationAndUser()
    {
        var workOs = new RecordingWorkOsService();
        var service = CreateService(workOs);

        await service.SendCrewInvitationAsync(
            "friend@example.com",
            CancellationToken.None
        );

        Assert.Equal("org_active", workOs.InvitationOrganizationId);
        Assert.Equal("user_active", workOs.InviterUserId);
        Assert.Equal("friend@example.com", workOs.InvitationEmailAddress);
    }

    [Fact]
    public async Task RemoveCrewMemberAsync_WhenCalled_ShouldUseActiveCrewOrganization()
    {
        var workOs = new RecordingWorkOsService();
        var service = CreateService(workOs);

        var wasRemoved = await service.RemoveCrewMemberAsync(
            "user_removed",
            CancellationToken.None
        );

        Assert.True(wasRemoved);
        Assert.Equal("org_active", workOs.DeactivatedMembershipOrganizationId);
        Assert.Equal("user_removed", workOs.DeactivatedMembershipUserId);
    }

    [Fact]
    public async Task LeaveCrewAsync_WhenCalled_ShouldDeactivateCurrentUserMembership()
    {
        var workOs = new RecordingWorkOsService();
        var service = CreateService(workOs);

        var wasRemoved = await service.LeaveCrewAsync(CancellationToken.None);

        Assert.True(wasRemoved);
        Assert.Equal("org_active", workOs.DeactivatedMembershipOrganizationId);
        Assert.Equal("user_active", workOs.DeactivatedMembershipUserId);
    }

    private static ICrewService CreateService(IWorkOsService workOs)
    {
        var crewUserContext = new CrewUserContext
        {
            OrganizationId = "org_active",
            UserId = "user_active",
        };

        return new WorkOsCrewService(workOs, crewUserContext);
    }

    private sealed class RecordingWorkOsService : IWorkOsService
    {
        public ActiveCrewMember[] ActiveMembers { get; init; } = [];

        public string? ActiveMembersOrganizationId { get; private set; }

        public string? CrewMembersOrganizationId { get; private set; }

        public string? InvitationOrganizationId { get; private set; }

        public string? InvitationEmailAddress { get; private set; }

        public string? InviterUserId { get; private set; }

        public string? DeactivatedMembershipOrganizationId { get; private set; }

        public string? DeactivatedMembershipUserId { get; private set; }

        public Task<string?> FindOrganizationByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> CreateOrganizationAsync(
            string name,
            string externalId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string?> FindActiveMembershipIdAsync(
            string userId,
            string organizationId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<string> CreateAdminMembershipAsync(
            string userId,
            string organizationId,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
            string organizationId,
            CancellationToken cancellationToken)
        {
            ActiveMembersOrganizationId = organizationId;

            return Task.FromResult(ActiveMembers);
        }

        public Task<CrewRoster> ListCrewMembersAsync(
            string organizationId,
            CancellationToken cancellationToken)
        {
            CrewMembersOrganizationId = organizationId;

            return Task.FromResult(new CrewRoster
            {
                ActiveMembers = [],
                PendingInvitations = [],
            });
        }

        public Task SendCrewInvitationAsync(
            string organizationId,
            string emailAddress,
            string inviterUserId,
            CancellationToken cancellationToken)
        {
            InvitationOrganizationId = organizationId;
            InvitationEmailAddress = emailAddress;
            InviterUserId = inviterUserId;

            return Task.CompletedTask;
        }

        public Task<bool> DeactivateOrganizationMembershipAsync(
            string organizationId,
            string userId,
            CancellationToken cancellationToken)
        {
            DeactivatedMembershipOrganizationId = organizationId;
            DeactivatedMembershipUserId = userId;

            return Task.FromResult(true);
        }
    }
}
