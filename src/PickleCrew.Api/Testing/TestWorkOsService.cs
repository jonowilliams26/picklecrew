using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Testing;

internal sealed class TestWorkOsService(HttpClient client)
    : IWorkOsProvisioningService
{
    public Task<string?> FindOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken)
    {
        var encodedExternalId = Uri.EscapeDataString(externalId);

        return FindIdAsync(
            $"/organizations/by-external-id/{encodedExternalId}",
            cancellationToken
        );
    }

    public Task<string> CreateOrganizationAsync(
        string name,
        string externalId,
        CancellationToken cancellationToken)
    {
        return CreateAsync(
            "/organizations",
            new CreateOrganizationHttpRequest
            {
                Name = name,
                ExternalId = externalId,
            },
            cancellationToken
        );
    }

    public Task<string?> FindActiveMembershipIdAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);

        return FindIdAsync(
            $"/organizations/{encodedOrganizationId}/active-memberships/{encodedUserId}",
            cancellationToken
        );
    }

    public Task<string> CreateAdminMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken)
    {
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);

        return CreateAsync(
            $"/organizations/{encodedOrganizationId}/memberships",
            new CreateMembershipHttpRequest
            {
                UserId = userId,
                Role = "admin",
            },
            cancellationToken
        );
    }

    private async Task<string?> FindIdAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return (await ReadResponseAsync(response, cancellationToken)).Id;
    }

    private async Task<string> CreateAsync<TRequest>(
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

        return (await ReadResponseAsync(response, cancellationToken)).Id;
    }

    private static async Task<IdentifierHttpResponse> ReadResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<IdentifierHttpResponse>(cancellationToken)
            ?? throw new InvalidOperationException(
                "The fake WorkOS service returned an empty response."
            );
    }

    private sealed record CreateOrganizationHttpRequest
    {
        public required string Name { get; init; }

        public required string ExternalId { get; init; }
    }

    private sealed record CreateMembershipHttpRequest
    {
        public required string UserId { get; init; }

        public required string Role { get; init; }
    }

    private sealed record IdentifierHttpResponse
    {
        public required string Id { get; init; }
    }
}
