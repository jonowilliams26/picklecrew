using PickleCrew.Api.Authorization;
using PickleCrew.Api.Services;

namespace PickleCrew.Api.WorkOs;

internal sealed class WorkOsCrewService(
    IWorkOsService workOs,
    CrewUserContext crewUserContext)
    : ICrewService
{
    public Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
        CancellationToken cancellationToken)
    {
        return workOs.ListActiveCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );
    }

    public async Task<Dictionary<string, ActiveCrewMember>> GetActiveMembersByIdAsync(
        CancellationToken cancellationToken)
    {
        var activeMembers = await ListActiveCrewMembersAsync(cancellationToken);

        return activeMembers.ToDictionary(
            member => member.WorkOsUserId,
            StringComparer.Ordinal
        );
    }

    public Task<CrewRoster> ListCrewMembersAsync(
        CancellationToken cancellationToken)
    {
        return workOs.ListCrewMembersAsync(
            crewUserContext.OrganizationId,
            cancellationToken
        );
    }

    public Task SendCrewInvitationAsync(
        string emailAddress,
        CancellationToken cancellationToken)
    {
        return workOs.SendCrewInvitationAsync(
            crewUserContext.OrganizationId,
            emailAddress,
            crewUserContext.UserId,
            cancellationToken
        );
    }

    public Task<bool> ChangeCrewMemberRoleAsync(
        string userId,
        string roleSlug,
        CancellationToken cancellationToken)
    {
        return workOs.UpdateOrganizationMembershipRoleAsync(
            crewUserContext.OrganizationId,
            userId,
            roleSlug,
            cancellationToken
        );
    }

    public Task<bool> RemoveCrewMemberAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return workOs.DeactivateOrganizationMembershipAsync(
            crewUserContext.OrganizationId,
            userId,
            cancellationToken
        );
    }

    public async Task<CrewLeavingEligibility> GetCrewLeavingEligibilityAsync(
        CancellationToken cancellationToken)
    {
        var activeMembers = await ListActiveCrewMembersAsync(cancellationToken);
        var currentMember = activeMembers.FirstOrDefault(member => string.Equals(
            member.WorkOsUserId,
            crewUserContext.UserId,
            StringComparison.Ordinal
        ));

        if (currentMember is null)
        {
            return CrewLeavingEligibility.MembershipNotFound;
        }

        var isAdministrator = string.Equals(
            currentMember.RoleSlug,
            WorkOsRoles.Admin,
            StringComparison.Ordinal
        );
        var hasOtherAdministrator = activeMembers.Any(member =>
            !string.Equals(
                member.WorkOsUserId,
                crewUserContext.UserId,
                StringComparison.Ordinal
            )
            && string.Equals(
                member.RoleSlug,
                WorkOsRoles.Admin,
                StringComparison.Ordinal
            )
        );

        return isAdministrator && !hasOtherAdministrator
            ? CrewLeavingEligibility.SoleAdministrator
            : CrewLeavingEligibility.Eligible;
    }

    public async Task<LeaveCrewOutcome> LeaveCrewAsync(
        CancellationToken cancellationToken)
    {
        var eligibility = await GetCrewLeavingEligibilityAsync(cancellationToken);

        if (eligibility == CrewLeavingEligibility.MembershipNotFound)
        {
            return LeaveCrewOutcome.MembershipNotFound;
        }

        if (eligibility == CrewLeavingEligibility.SoleAdministrator)
        {
            return LeaveCrewOutcome.SoleAdministrator;
        }

        var wasMembershipDeactivated = await workOs.DeactivateOrganizationMembershipAsync(
            crewUserContext.OrganizationId,
            crewUserContext.UserId,
            cancellationToken
        );

        return wasMembershipDeactivated
            ? LeaveCrewOutcome.Left
            : LeaveCrewOutcome.MembershipNotFound;
    }
}
