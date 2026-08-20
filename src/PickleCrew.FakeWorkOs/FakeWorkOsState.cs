using System.Collections.Concurrent;

namespace PickleCrew.FakeWorkOs;

internal sealed class FakeWorkOsState
{
    private readonly ConcurrentDictionary<string, FakeUser> users = new();
    private readonly ConcurrentDictionary<string, FakeOrganization> organizations = new();
    private readonly ConcurrentDictionary<FakeOperationFaultKey, byte> faults = new();

    public FakeUser CreateUser(
        string id,
        string? emailAddress = null,
        string? firstName = null,
        string? lastName = null,
        string? displayName = null,
        string? profilePictureUrl = null)
    {
        return users.GetOrAdd(
            id,
            userId => new FakeUser
            {
                Id = userId,
                EmailAddress = emailAddress ?? $"{userId}@picklecrew.test",
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                ProfilePictureUrl = profilePictureUrl,
            }
        );
    }

    public FakeOrganization? FindOrganization(string externalId)
    {
        organizations.TryGetValue(externalId, out var organization);

        return organization;
    }

    public FakeOrganization CreateOrganization(
        string name,
        string externalId,
        string? id = null)
    {
        return organizations.GetOrAdd(
            externalId,
            _ => new FakeOrganization
            {
                Id = id ?? $"org_{Guid.NewGuid():N}",
                Name = name,
                ExternalId = externalId,
            }
        );
    }

    public FakeMembership? FindActiveMembership(string userId, string organizationId)
    {
        var organization = FindOrganizationById(organizationId);
        if (organization is null)
        {
            return null;
        }

        organization.Memberships.TryGetValue(userId, out var membership);

        return membership?.Status == MembershipStatus.Active ? membership : null;
    }

    public FakeActiveCrewMember[] ListActiveMembers(string organizationId)
    {
        var organization = GetOrganizationById(organizationId);

        return organization.Memberships.Values
            .Where(membership => membership.Status == MembershipStatus.Active)
            .Select(membership => users[membership.UserId])
            .Select(user => new FakeActiveCrewMember
            {
                WorkOsUserId = user.Id,
                DisplayName = ResolveDisplayName(user),
                EmailAddress = user.EmailAddress,
                ProfilePictureUrl = user.ProfilePictureUrl,
            })
            .OrderBy(member => member.WorkOsUserId, StringComparer.Ordinal)
            .ToArray();
    }

    public FakeMembership CreateMembership(
        string userId,
        string organizationId,
        MembershipRole role,
        MembershipStatus status = MembershipStatus.Active,
        string? id = null)
    {
        var user = CreateUser(userId);
        var organization = GetOrganizationById(organizationId);

        return organization.Memberships.GetOrAdd(
            user.Id,
            _ => new FakeMembership
            {
                Id = id ?? $"om_{Guid.NewGuid():N}",
                UserId = user.Id,
                Role = role,
                Status = status,
            }
        );
    }

    public FakeInvitation CreateInvitation(
        string organizationId,
        string emailAddress,
        MembershipRole role,
        string inviterUserId,
        InvitationState state = InvitationState.Pending,
        string? id = null)
    {
        var organization = GetOrganizationById(organizationId);
        var invitation = new FakeInvitation
        {
            Id = id ?? $"invitation_{Guid.NewGuid():N}",
            OrganizationId = organization.Id,
            EmailAddress = emailAddress.Trim(),
            State = state,
            Role = role,
            InviterUserId = inviterUserId,
        };

        if (!organization.Invitations.TryAdd(invitation.Id, invitation))
        {
            throw new InvalidOperationException(
                $"Invitation '{invitation.Id}' already exists."
            );
        }

        return invitation;
    }

    public FakeInvitation[] ListInvitations(string organizationId)
    {
        var organization = GetOrganizationById(organizationId);

        return organization.Invitations.Values
            .OrderBy(invitation => invitation.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public FakeInvitation[] ListPendingInvitations(string organizationId)
    {
        return ListInvitations(organizationId)
            .Where(invitation => invitation.State == InvitationState.Pending)
            .ToArray();
    }

    public void FailOperation(
        string scenarioId,
        FakeWorkOsOperation operation,
        string? recipientEmailAddress = null)
    {
        faults.TryAdd(
            new FakeOperationFaultKey
            {
                ScenarioId = scenarioId,
                Operation = operation,
                RecipientEmailAddress = NormalizeEmailAddress(recipientEmailAddress),
            },
            0
        );
    }

    public bool ShouldFailOperation(
        string scenarioId,
        FakeWorkOsOperation operation,
        string? recipientEmailAddress = null)
    {
        var operationFault = new FakeOperationFaultKey
        {
            ScenarioId = scenarioId,
            Operation = operation,
        };
        var recipientFault = operationFault with
        {
            RecipientEmailAddress = NormalizeEmailAddress(recipientEmailAddress),
        };

        return faults.ContainsKey(operationFault)
            || faults.ContainsKey(recipientFault);
    }

    public FakeOrganizationWithUser CreateOrganizationWithUser(
        string userId,
        string userEmailAddress,
        string organizationName,
        string organizationExternalId,
        MembershipRole role,
        string? firstName = null,
        string? lastName = null,
        string? displayName = null,
        string? profilePictureUrl = null)
    {
        var user = CreateUser(
            userId,
            userEmailAddress,
            firstName,
            lastName,
            displayName,
            profilePictureUrl
        );
        var organization = CreateOrganization(organizationName, organizationExternalId);
        var membership = CreateMembership(user.Id, organization.Id, role);

        return new FakeOrganizationWithUser
        {
            User = user,
            Organization = organization,
            Membership = membership,
        };
    }

    private static string ResolveDisplayName(FakeUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        var nameParts = new[] { user.FirstName, user.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());
        var name = string.Join(' ', nameParts);

        return string.IsNullOrWhiteSpace(name)
            ? user.EmailAddress
            : name;
    }

    private FakeOrganization GetOrganizationById(string organizationId)
    {
        return FindOrganizationById(organizationId)
            ?? throw new InvalidOperationException(
                $"Organization '{organizationId}' does not exist."
            );
    }

    private FakeOrganization? FindOrganizationById(string organizationId)
    {
        return organizations.Values.FirstOrDefault(
            candidate => candidate.Id == organizationId
        );
    }

    private static string? NormalizeEmailAddress(string? emailAddress)
    {
        return string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim().ToUpperInvariant();
    }
}

internal sealed record FakeOperationFaultKey
{
    public required string ScenarioId { get; init; }

    public required FakeWorkOsOperation Operation { get; init; }

    public string? RecipientEmailAddress { get; init; }
}

internal sealed record FakeUser
{
    public required string Id { get; init; }

    public required string EmailAddress { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DisplayName { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed record FakeActiveCrewMember
{
    public required string WorkOsUserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed record FakeOrganization
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ExternalId { get; init; }

    public ConcurrentDictionary<string, FakeMembership> Memberships { get; } = new();

    public ConcurrentDictionary<string, FakeInvitation> Invitations { get; } = new();
}

internal sealed record FakeMembership
{
    public required string Id { get; init; }

    public required string UserId { get; init; }

    public required MembershipRole Role { get; init; }

    public required MembershipStatus Status { get; init; }
}

internal sealed record FakeInvitation
{
    public required string Id { get; init; }

    public required string OrganizationId { get; init; }

    public required string EmailAddress { get; init; }

    public required InvitationState State { get; init; }

    public required MembershipRole Role { get; init; }

    public required string InviterUserId { get; init; }
}

internal sealed record FakeOrganizationWithUser
{
    public required FakeUser User { get; init; }

    public required FakeOrganization Organization { get; init; }

    public required FakeMembership Membership { get; init; }
}
