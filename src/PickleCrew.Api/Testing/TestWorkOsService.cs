using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Authorization;
using PickleCrew.Api.WorkOs;

namespace PickleCrew.Api.Testing;

internal static class TestScenarioDefaults
{
    public const string ScenarioIdHeaderName = "X-Test-Scenario-Id";
}

internal sealed class TestWorkOsService : IWorkOsService
{
    private readonly HttpClient client;

    public TestWorkOsService(
        HttpClient client,
        IHttpContextAccessor httpContextAccessor)
    {
        this.client = client;

        var scenarioId = httpContextAccessor.HttpContext?
            .Request.Headers[TestScenarioDefaults.ScenarioIdHeaderName]
            .ToString();

        if (!string.IsNullOrWhiteSpace(scenarioId))
        {
            client.DefaultRequestHeaders.Add(
                TestScenarioDefaults.ScenarioIdHeaderName,
                scenarioId
            );
        }
    }

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
        return CreateAsync(
            "/memberships",
            new CreateMembershipHttpRequest
            {
                UserId = userId,
                OrganizationId = organizationId,
                Role = WorkOsRoles.Admin,
            },
            cancellationToken
        );
    }

    public async Task<ActiveCrewMember[]> ListActiveCrewMembersAsync(
        string organizationId,
        CancellationToken cancellationToken)
    {
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);
        using var response = await client.GetAsync(
            $"/organizations/{encodedOrganizationId}/active-members",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ActiveCrewMemberListHttpResponse>(
            cancellationToken
        );

        return result?.Members
            .Select(member => new ActiveCrewMember
            {
                WorkOsUserId = member.WorkOsUserId,
                DisplayName = member.DisplayName,
                EmailAddress = member.EmailAddress,
                RoleSlug = member.Role,
                ProfilePictureUrl = member.ProfilePictureUrl,
            })
            .ToArray()
            ?? throw new InvalidOperationException(
                "The fake WorkOS service returned an empty active member list response."
            );
    }

    public async Task<string[]> ListPendingCrewInvitationEmailAddressesAsync(
        string organizationId,
        CancellationToken cancellationToken)
    {
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);
        using var response = await client.GetAsync(
            $"/organizations/{encodedOrganizationId}/pending-invitations",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InvitationListHttpResponse>(
            cancellationToken
        );

        return result?.Invitations
            .Select(invitation => invitation.EmailAddress)
            .ToArray()
            ?? throw new InvalidOperationException(
                "The fake WorkOS service returned an empty invitation list response."
            );
    }

    public async Task SendCrewInvitationAsync(
        string organizationId,
        string emailAddress,
        string inviterUserId,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/invitations",
            new CreateInvitationHttpRequest
            {
                OrganizationId = organizationId,
                EmailAddress = emailAddress,
                Role = WorkOsRoles.Member,
                InviterUserId = inviterUserId,
            },
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
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

        public required string OrganizationId { get; init; }

        public required string Role { get; init; }
    }

    private sealed record CreateInvitationHttpRequest
    {
        public required string OrganizationId { get; init; }

        public required string EmailAddress { get; init; }

        public required string Role { get; init; }

        public required string InviterUserId { get; init; }
    }

    private sealed record IdentifierHttpResponse
    {
        public required string Id { get; init; }
    }

    private sealed record ActiveCrewMemberListHttpResponse
    {
        public required ActiveCrewMemberHttpResponse[] Members { get; init; }
    }

    private sealed record ActiveCrewMemberHttpResponse
    {
        public required string WorkOsUserId { get; init; }

        public required string DisplayName { get; init; }

        public required string EmailAddress { get; init; }

        public required string Role { get; init; }

        public string? ProfilePictureUrl { get; init; }
    }

    private sealed record InvitationListHttpResponse
    {
        public required InvitationHttpResponse[] Invitations { get; init; }
    }

    private sealed record InvitationHttpResponse
    {
        public required string EmailAddress { get; init; }
    }
}
