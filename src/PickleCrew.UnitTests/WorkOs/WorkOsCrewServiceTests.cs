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
        public string? ActiveMembersOrganizationId { get; private set; }

        public string? CrewMembersOrganizationId { get; private set; }

        public string? InvitationOrganizationId { get; private set; }

        public string? InvitationEmailAddress { get; private set; }

        public string? InviterUserId { get; private set; }

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

            return Task.FromResult(Array.Empty<ActiveCrewMember>());
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
    }
}
