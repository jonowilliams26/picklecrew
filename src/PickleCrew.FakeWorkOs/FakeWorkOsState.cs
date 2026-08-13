using System.Collections.Concurrent;

namespace PickleCrew.FakeWorkOs;

internal sealed class FakeWorkOsState
{
    private readonly ConcurrentDictionary<string, FakeUser> users = new();
    private readonly ConcurrentDictionary<string, FakeOrganization> organizations = new();
    private readonly ConcurrentDictionary<FakeOperationFaultKey, byte> faults = new();

    public FakeUser CreateUser(string id)
    {
        return users.GetOrAdd(id, userId => new FakeUser { Id = userId });
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

    public FakeMembership CreateMembership(
        string userId,
        string organizationId,
        MembershipRole role,
        MembershipStatus status = MembershipStatus.Active,
        string? id = null)
    {
        var user = CreateUser(userId);
        var organization = FindOrganizationById(organizationId)
            ?? throw new InvalidOperationException(
                $"Organization '{organizationId}' does not exist."
            );

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

    public void FailOperation(string scenarioId, FakeWorkOsOperation operation)
    {
        faults.TryAdd(
            new FakeOperationFaultKey
            {
                ScenarioId = scenarioId,
                Operation = operation,
            },
            0
        );
    }

    public bool ShouldFailOperation(string scenarioId, FakeWorkOsOperation operation)
    {
        return faults.ContainsKey(
            new FakeOperationFaultKey
            {
                ScenarioId = scenarioId,
                Operation = operation,
            }
        );
    }

    public FakeOrganizationWithUser CreateOrganizationWithUser(
        string userId,
        string organizationName,
        string organizationExternalId,
        MembershipRole role)
    {
        var user = CreateUser(userId);
        var organization = CreateOrganization(organizationName, organizationExternalId);
        var membership = CreateMembership(user.Id, organization.Id, role);

        return new FakeOrganizationWithUser
        {
            User = user,
            Organization = organization,
            Membership = membership,
        };
    }

    private FakeOrganization? FindOrganizationById(string organizationId)
    {
        return organizations.Values.FirstOrDefault(
            candidate => candidate.Id == organizationId
        );
    }
}

internal sealed record FakeOperationFaultKey
{
    public required string ScenarioId { get; init; }

    public required FakeWorkOsOperation Operation { get; init; }
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

    public required MembershipStatus Status { get; init; }
}

internal sealed record FakeOrganizationWithUser
{
    public required FakeUser User { get; init; }

    public required FakeOrganization Organization { get; init; }

    public required FakeMembership Membership { get; init; }
}
