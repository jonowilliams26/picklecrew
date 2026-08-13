using System.Collections.Concurrent;

namespace PickleCrew.FakeWorkOs;

internal sealed class FakeWorkOsState
{
    private readonly ConcurrentDictionary<string, FakeUser> users = new();
    private readonly ConcurrentDictionary<string, FakeOrganization> organizations = new();
    private readonly ConcurrentDictionary<FakeOperationOutcomeKey, FakeOperationOutcome> outcomes = new();

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

    public FakeOperationOutcome CreateOutcome(
        FakeWorkOsOperation operation,
        string scope,
        int statusCode,
        string detail)
    {
        return outcomes.GetOrAdd(
            new FakeOperationOutcomeKey(operation, scope),
            _ => new FakeOperationOutcome
            {
                StatusCode = statusCode,
                Detail = detail,
            }
        );
    }

    public FakeOperationOutcome? FindOutcome(
        FakeWorkOsOperation operation,
        string scope)
    {
        outcomes.TryGetValue(new FakeOperationOutcomeKey(operation, scope), out var outcome);

        return outcome;
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

internal readonly record struct FakeOperationOutcomeKey(
    FakeWorkOsOperation Operation,
    string Scope);

internal sealed record FakeOperationOutcome
{
    public required int StatusCode { get; init; }

    public required string Detail { get; init; }
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
