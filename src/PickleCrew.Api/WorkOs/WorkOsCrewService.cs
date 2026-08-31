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
}
