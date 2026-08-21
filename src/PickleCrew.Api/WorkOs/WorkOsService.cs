using PickleCrew.Api.Authorization;
using WorkOS;

namespace PickleCrew.Api.WorkOs;

internal sealed record ActiveCrewMember
{
    public required string WorkOsUserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required string RoleSlug { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal interface IWorkOsService
{
    Task<string?> FindOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken);

    Task<string> CreateOrganizationAsync(
        string name,
        string externalId,
        CancellationToken cancellationToken);

    Task<string?> FindActiveMembershipIdAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken);

    Task<string> CreateAdminMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken);

    Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
        string organizationId,
        CancellationToken cancellationToken);

    Task<string[]> ListPendingCrewInvitationEmailAddressesAsync(
        string organizationId,
        CancellationToken cancellationToken);

    Task SendCrewInvitationAsync(
        string organizationId,
        string emailAddress,
        string inviterUserId,
        CancellationToken cancellationToken);
}

internal sealed class WorkOsService(WorkOSClient client)
    : IWorkOsService
{
    public async Task<string?> FindOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        try
        {
            var organization = await client.Organizations.GetByExternalIdAsync(
                externalId,
                cancellationToken: cancellationToken);

            return organization.Id;
        }
        catch (WorkOS.NotFoundException)
        {
            return null;
        }
    }

    public async Task<string> CreateOrganizationAsync(
        string name,
        string externalId,
        CancellationToken cancellationToken)
    {
        var organization = await client.Organizations.CreateAsync(
            new OrganizationsCreateOptions
            {
                Name = name,
                ExternalId = externalId,
            },
            cancellationToken: cancellationToken);

        return organization.Id;
    }

    public async Task<string?> FindActiveMembershipIdAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken)
    {
        var memberships = await client.OrganizationMembership.ListAsync(
            new OrganizationMembershipListOptions
            {
                UserId = userId,
                OrganizationId = organizationId,
                Statuses = [OrganizationMembershipCreatedDataStatus.Active],
            },
            cancellationToken: cancellationToken);

        return memberships.Data?.FirstOrDefault()?.Id;
    }

    public async Task<string> CreateAdminMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken)
    {
        var membership = await client.OrganizationMembership.CreateAsync(
            new OrganizationMembershipCreateOptions
            {
                UserId = userId,
                OrganizationId = organizationId,
                Role = new OrganizationMembershipRoleSingle
                {
                    RoleSlug = WorkOsRoles.Admin,
                },
            },
            cancellationToken: cancellationToken);

        return membership.Id;
    }

    public async Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
        string organizationId,
        CancellationToken cancellationToken)
    {
        var activeMembers = new List<ActiveCrewMember>();
        var memberships = client.OrganizationMembership.ListAutoPagingAsync(
            new OrganizationMembershipListOptions
            {
                OrganizationId = organizationId,
                Statuses = [OrganizationMembershipCreatedDataStatus.Active],
            },
            cancellationToken: cancellationToken
        );

        await foreach (var membership in memberships)
        {
            activeMembers.Add(new ActiveCrewMember
            {
                WorkOsUserId = membership.User.Id,
                DisplayName = GetDisplayName(membership.User),
                EmailAddress = membership.User.Email,
                RoleSlug = membership.Role.Slug,
                ProfilePictureUrl = membership.User.ProfilePictureUrl,
            });
        }

        return activeMembers.ToArray();
    }

    public async Task<string[]> ListPendingCrewInvitationEmailAddressesAsync(
        string organizationId,
        CancellationToken cancellationToken)
    {
        var emailAddresses = new List<string>();
        var invitations = client.UserManagement.ListInvitationsAutoPagingAsync(
            new UserManagementListInvitationsOptions
            {
                OrganizationId = organizationId,
            },
            cancellationToken: cancellationToken
        );

        await foreach (var invitation in invitations)
        {
            if (invitation.State == InvitationAcceptedDataState.Pending)
            {
                emailAddresses.Add(invitation.Email);
            }
        }

        return emailAddresses.ToArray();
    }

    private static string GetDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Name))
        {
            return user.Name.Trim();
        }

        var nameParts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());
        var displayName = string.Join(' ', nameParts);

        return string.IsNullOrWhiteSpace(displayName)
            ? user.Email
            : displayName;
    }

    public async Task SendCrewInvitationAsync(
        string organizationId,
        string emailAddress,
        string inviterUserId,
        CancellationToken cancellationToken)
    {
        await client.UserManagement.SendInvitationAsync(
            new UserManagementSendInvitationOptions
            {
                Email = emailAddress,
                OrganizationId = organizationId,
                RoleSlug = WorkOsRoles.Member,
                InviterUserId = inviterUserId,
            },
            new RequestOptions
            {
                MaxRetries = 0,
            },
            cancellationToken
        );
    }
}
