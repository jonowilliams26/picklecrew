namespace PickleCrew.Api.Services;

internal sealed record ActiveCrewMember
{
    public required string WorkOsUserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required string RoleSlug { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed record PendingCrewInvitation
{
    public required string WorkOsInvitationId { get; init; }

    public required string EmailAddress { get; init; }
}

internal sealed record CrewRoster
{
    public required ActiveCrewMember[] ActiveMembers { get; init; }

    public required PendingCrewInvitation[] PendingInvitations { get; init; }
}

internal interface ICrewService
{
    Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
        CancellationToken cancellationToken);

    Task<Dictionary<string, ActiveCrewMember>> GetActiveMembersByIdAsync(
        CancellationToken cancellationToken);

    Task<CrewRoster> ListCrewMembersAsync(
        CancellationToken cancellationToken);

    Task SendCrewInvitationAsync(
        string emailAddress,
        CancellationToken cancellationToken);

    Task<bool> RemoveCrewMemberAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<bool> LeaveCrewAsync(
        CancellationToken cancellationToken);
}
