using System.Net;
using System.Net.Http.Json;

namespace PickleCrew.IntegrationTests.Testing;

internal sealed class FakeWorkOsClient(HttpClient client) : IDisposable
{
    public async Task<FakeWorkOsSetup> SetupAsync(
        string userId,
        string organizationName,
        string organizationExternalId,
        FakeMembershipRole role,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/setup",
            new SetupHttpRequest
            {
                UserId = userId,
                OrganizationName = organizationName,
                OrganizationExternalId = organizationExternalId,
                Role = role.ToString(),
            },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<FakeWorkOsSetup>(cancellationToken)
            ?? throw new InvalidOperationException(
                "The fake WorkOS service returned an empty setup response."
            );
    }

    public Task<string> CreateUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return PostAndReadIdAsync(
            "/users",
            new CreateUserHttpRequest { Id = userId },
            cancellationToken
        );
    }

    public Task<string> CreateOrganizationAsync(
        string organizationId,
        string name,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return PostAndReadIdAsync(
            "/organizations",
            new CreateOrganizationHttpRequest
            {
                Id = organizationId,
                Name = name,
                ExternalId = externalId,
            },
            cancellationToken
        );
    }

    public Task<string> CreateMembershipAsync(
        string membershipId,
        string userId,
        string organizationId,
        FakeMembershipRole role,
        FakeMembershipStatus status,
        CancellationToken cancellationToken = default)
    {
        return PostAndReadIdAsync(
            "/memberships",
            new CreateMembershipHttpRequest
            {
                Id = membershipId,
                UserId = userId,
                OrganizationId = organizationId,
                Role = role.ToString(),
                Status = status.ToString(),
            },
            cancellationToken
        );
    }

    public async Task CreateOutcomeAsync(
        FakeWorkOsOperation operation,
        string scope,
        HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/outcomes",
            new CreateOutcomeHttpRequest
            {
                Operation = operation.ToString(),
                Scope = scope,
                StatusCode = (int)statusCode,
                Detail = $"Configured {operation} failure for '{scope}'.",
            },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public async Task<string?> FindOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetOrganizationByExternalIdAsync(
            externalId,
            cancellationToken
        );

        return await ReadOptionalIdAsync(response, cancellationToken);
    }

    public Task<HttpResponseMessage> GetOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var encodedExternalId = Uri.EscapeDataString(externalId);

        return client.GetAsync(
            $"/organizations/by-external-id/{encodedExternalId}",
            cancellationToken
        );
    }

    public async Task<string?> FindActiveMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await GetActiveMembershipAsync(
            userId,
            organizationId,
            cancellationToken
        );

        return await ReadOptionalIdAsync(response, cancellationToken);
    }

    public Task<HttpResponseMessage> GetActiveMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);

        return client.GetAsync(
            $"/organizations/{encodedOrganizationId}/active-memberships/{encodedUserId}",
            cancellationToken
        );
    }

    public Task<HttpResponseMessage> SendCreateOrganizationAsync(
        string name,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync(
            "/organizations",
            new CreateOrganizationHttpRequest
            {
                Name = name,
                ExternalId = externalId,
            },
            cancellationToken
        );
    }

    public Task<HttpResponseMessage> SendCreateMembershipAsync(
        string userId,
        string organizationId,
        FakeMembershipRole role,
        CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync(
            "/memberships",
            new CreateMembershipHttpRequest
            {
                UserId = userId,
                OrganizationId = organizationId,
                Role = role.ToString(),
                Status = FakeMembershipStatus.Active.ToString(),
            },
            cancellationToken
        );
    }

    private async Task<string> PostAndReadIdAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            requestUri,
            request,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        return await ReadIdAsync(response, cancellationToken);
    }

    private static async Task<string?> ReadOptionalIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await ReadIdAsync(response, cancellationToken);
    }

    private static async Task<string> ReadIdAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadFromJsonAsync<IdentifierHttpResponse>(
            cancellationToken
        );

        return content?.Id ?? throw new InvalidOperationException(
            "The fake WorkOS service returned an empty identifier response."
        );
    }

    public void Dispose()
    {
        client.Dispose();
    }

    private sealed record SetupHttpRequest
    {
        public required string UserId { get; init; }

        public required string OrganizationName { get; init; }

        public required string OrganizationExternalId { get; init; }

        public required string Role { get; init; }
    }

    private sealed record CreateUserHttpRequest
    {
        public required string Id { get; init; }
    }

    private sealed record CreateOrganizationHttpRequest
    {
        public string? Id { get; init; }

        public required string Name { get; init; }

        public required string ExternalId { get; init; }
    }

    private sealed record CreateMembershipHttpRequest
    {
        public string? Id { get; init; }

        public required string UserId { get; init; }

        public required string OrganizationId { get; init; }

        public required string Role { get; init; }

        public required string Status { get; init; }
    }

    private sealed record CreateOutcomeHttpRequest
    {
        public required string Operation { get; init; }

        public required string Scope { get; init; }

        public required int StatusCode { get; init; }

        public required string Detail { get; init; }
    }

    private sealed record IdentifierHttpResponse
    {
        public required string Id { get; init; }
    }
}

internal sealed record FakeWorkOsSetup
{
    public required string UserId { get; init; }

    public required string OrganizationId { get; init; }

    public required string MembershipId { get; init; }
}

internal enum FakeMembershipRole
{
    Admin,
    Member,
}

internal enum FakeMembershipStatus
{
    Active,
    Inactive,
}

public enum FakeWorkOsOperation
{
    FindOrganizationByExternalId,
    CreateOrganization,
    FindActiveMembership,
    CreateMembership,
}
