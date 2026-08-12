using System.Collections.Concurrent;

namespace PickleCrew.FakeWorkOs;

internal sealed class FakeWorkOsState
{
    private readonly ConcurrentDictionary<string, FakeUser> users = new();
    private readonly ConcurrentDictionary<string, FakeOrganization> organizations = new();

    public FakeOrganization? FindOrganization(string externalId)
    {
        organizations.TryGetValue(externalId, out var organization);

        return organization;
    }

    public FakeOrganization CreateOrganization(string name, string externalId)
    {
        return organizations.GetOrAdd(
            externalId,
            _ => new FakeOrganization
            {
                Id = $"org_{Guid.NewGuid():N}",
                Name = name,
                ExternalId = externalId,
            }
        );
    }

    public FakeMembership? FindActiveMembership(string userId, string organizationId)
    {
        var organization = organizations.Values.FirstOrDefault(
            candidate => candidate.Id == organizationId
        );
        if (organization is null)
        {
            return null;
        }

        organization.Memberships.TryGetValue(userId, out var membership);

        return membership;
    }

    public FakeMembership CreateMembership(
        string userId,
        string organizationId,
        MembershipRole role)
    {
        var user = users.GetOrAdd(userId, id => new FakeUser { Id = id });
        var organization = organizations.Values.SingleOrDefault(
            candidate => candidate.Id == organizationId
        ) ?? throw new InvalidOperationException(
            $"Organization '{organizationId}' does not exist."
        );

        return organization.Memberships.GetOrAdd(
            user.Id,
            _ => new FakeMembership
            {
                Id = $"om_{Guid.NewGuid():N}",
                UserId = user.Id,
                Role = role,
            }
        );
    }

    public FakeOrganizationWithUser CreateOrganizationWithUser(
        string userId,
        string organizationName,
        string organizationExternalId,
        MembershipRole role)
    {
        var user = users.GetOrAdd(userId, id => new FakeUser { Id = id });
        var organization = CreateOrganization(organizationName, organizationExternalId);
        var membership = CreateMembership(user.Id, organization.Id, role);

        return new FakeOrganizationWithUser
        {
            User = user,
            Organization = organization,
            Membership = membership,
        };
    }
}

internal sealed record FakeUser
{
    public required string Id { get; init; }
}

internal sealed record FakeOrganization
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ExternalId { get; init; }

    public ConcurrentDictionary<string, FakeMembership> Memberships { get; } = new();
}

internal sealed record FakeMembership
{
    public required string Id { get; init; }

    public required string UserId { get; init; }

    public required MembershipRole Role { get; init; }
}

internal sealed record FakeOrganizationWithUser
{
    public required FakeUser User { get; init; }

    public required FakeOrganization Organization { get; init; }

    public required FakeMembership Membership { get; init; }
}
