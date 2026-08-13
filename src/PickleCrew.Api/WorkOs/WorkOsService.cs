using WorkOS;

namespace PickleCrew.Api.WorkOs;

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
                    RoleSlug = "admin",
                },
            },
            cancellationToken: cancellationToken);

        return membership.Id;
    }
}
