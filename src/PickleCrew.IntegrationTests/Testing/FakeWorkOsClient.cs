using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Testing;

namespace PickleCrew.IntegrationTests.Testing;

internal sealed class FakeWorkOsClient(HttpClient client) : IDisposable
{
    public async Task<FakeWorkOsSetup> SetupAsync(
        string userId,
        string userEmailAddress,
        string organizationName,
        string organizationExternalId,
        FakeMembershipRole role,
        string? userFirstName = null,
        string? userLastName = null,
        string? userDisplayName = null,
        string? userProfilePictureUrl = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsJsonAsync(
            "/setup",
            new SetupHttpRequest
            {
                UserId = userId,
                UserEmailAddress = userEmailAddress,
                UserFirstName = userFirstName,
                UserLastName = userLastName,
                UserDisplayName = userDisplayName,
                UserProfilePictureUrl = userProfilePictureUrl,
                OrganizationName = organizationName,
                OrganizationExternalId = organizationExternalId,
                Role = role,
            },
            TestJsonSerializerOptions.Default,
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
        string emailAddress,
        string? firstName = null,
        string? lastName = null,
        string? displayName = null,
        string? profilePictureUrl = null,
        CancellationToken cancellationToken = default)
    {
        return PostAndReadIdAsync(
            "/users",
            new CreateUserHttpRequest
            {
                Id = userId,
                EmailAddress = emailAddress,
                FirstName = firstName,
                LastName = lastName,
                DisplayName = displayName,
                ProfilePictureUrl = profilePictureUrl,
            },
            cancellationToken
        );
    }

    public async Task UpdateUserAsync(
        string userId,
        string displayName,
        string? profilePictureUrl = null,
        CancellationToken cancellationToken = default)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        using var response = await client.PutAsJsonAsync(
            $"/users/{encodedUserId}",
            new UpdateUserHttpRequest
            {
                DisplayName = displayName,
                ProfilePictureUrl = profilePictureUrl,
            },
            TestJsonSerializerOptions.Default,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
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

    public async Task DeleteOrganizationAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.DeleteAsync(
            $"/organizations/{Uri.EscapeDataString(organizationId)}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
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
                Role = role,
                Status = status,
            },
            cancellationToken
        );
    }

    public async Task UpdateMembershipRoleAsync(
        string membershipId,
        FakeMembershipRole role,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PutAsJsonAsync(
            $"/memberships/{Uri.EscapeDataString(membershipId)}",
            new UpdateMembershipRoleHttpRequest { Role = role },
            TestJsonSerializerOptions.Default,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public async Task DeactivateMembershipAsync(
        string membershipId,
        CancellationToken cancellationToken = default)
    {
        using var response = await client.PostAsync(
            $"/memberships/{Uri.EscapeDataString(membershipId)}/deactivate",
            content: null,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    public Task<FakeInvitation> CreateInvitationAsync(
        string invitationId,
        string organizationId,
        string emailAddress,
        FakeMembershipRole role,
        string inviterUserId,
        FakeInvitationState state = FakeInvitationState.Pending,
        CancellationToken cancellationToken = default)
    {
        return PostAndReadAsync<CreateInvitationHttpRequest, FakeInvitation>(
            "/invitations",
            new CreateInvitationHttpRequest
            {
                Id = invitationId,
                OrganizationId = organizationId,
                EmailAddress = emailAddress,
                Role = role,
                InviterUserId = inviterUserId,
                State = state,
            },
            cancellationToken
        );
    }

    public async Task FailOperationAsync(
        string scenarioId,
        FakeWorkOsOperation operation,
        CancellationToken cancellationToken = default)
    {
        await ConfigureFaultAsync(
            new FailOperationHttpRequest
            {
                ScenarioId = scenarioId,
                Operation = operation,
            },
            cancellationToken
        );
    }

    public async Task FailInvitationAsync(
        string scenarioId,
        string recipientEmailAddress,
        CancellationToken cancellationToken = default)
    {
        await ConfigureFaultAsync(
            new FailOperationHttpRequest
            {
                ScenarioId = scenarioId,
                Operation = FakeWorkOsOperation.SendInvitation,
                RecipientEmailAddress = recipientEmailAddress,
            },
            cancellationToken
        );
    }

    public async Task<string?> FindOrganizationByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var encodedExternalId = Uri.EscapeDataString(externalId);
        using var response = await client.GetAsync(
            $"/organizations/by-external-id/{encodedExternalId}",
            cancellationToken
        );

        return await ReadOptionalIdAsync(response, cancellationToken);
    }

    public Task<HttpResponseMessage> GetOrganizationByExternalIdAsync(
        string externalId,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var encodedExternalId = Uri.EscapeDataString(externalId);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/organizations/by-external-id/{encodedExternalId}"
        );

        return SendAsync(request, scenarioId, cancellationToken);
    }

    public async Task<string?> FindActiveMembershipAsync(
        string userId,
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);
        using var response = await client.GetAsync(
            $"/organizations/{encodedOrganizationId}/active-memberships/{encodedUserId}",
            cancellationToken
        );

        return await ReadOptionalIdAsync(response, cancellationToken);
    }

    public Task<HttpResponseMessage> GetActiveMembershipAsync(
        string userId,
        string organizationId,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var encodedUserId = Uri.EscapeDataString(userId);
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/organizations/{encodedOrganizationId}/active-memberships/{encodedUserId}"
        );

        return SendAsync(request, scenarioId, cancellationToken);
    }

    public async Task<FakeActiveCrewMember[]> ListActiveMembersAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var encodedOrganizationId = Uri.EscapeDataString(organizationId);
        using var response = await client.GetAsync(
            $"/organizations/{encodedOrganizationId}/active-members",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ActiveCrewMemberListHttpResponse>(
            TestJsonSerializerOptions.Default,
            cancellationToken
        );

        return result?.Members ?? throw new InvalidOperationException(
            "The fake WorkOS service returned an empty active member list response."
        );
    }

    public Task<FakeInvitation[]> ListPendingInvitationsAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        return ReadInvitationListAsync(
            $"/organizations/{Uri.EscapeDataString(organizationId)}/pending-invitations",
            cancellationToken
        );
    }

    public Task<FakeInvitation[]> ListInvitationsAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        return ReadInvitationListAsync(
            $"/organizations/{Uri.EscapeDataString(organizationId)}/invitations",
            cancellationToken
        );
    }

    public Task<HttpResponseMessage> SendCreateOrganizationAsync(
        string name,
        string externalId,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/organizations")
        {
            Content = JsonContent.Create(new CreateOrganizationHttpRequest
            {
                Name = name,
                ExternalId = externalId,
            }),
        };

        return SendAsync(request, scenarioId, cancellationToken);
    }

    public Task<HttpResponseMessage> SendCreateMembershipAsync(
        string userId,
        string organizationId,
        FakeMembershipRole role,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/memberships")
        {
            Content = JsonContent.Create(
                new CreateMembershipHttpRequest
                {
                    UserId = userId,
                    OrganizationId = organizationId,
                    Role = role,
                    Status = FakeMembershipStatus.Active,
                },
                options: TestJsonSerializerOptions.Default
            ),
        };

        return SendAsync(request, scenarioId, cancellationToken);
    }

    public Task<HttpResponseMessage> SendInvitationAsync(
        string organizationId,
        string emailAddress,
        FakeMembershipRole role,
        string inviterUserId,
        string scenarioId,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/invitations")
        {
            Content = JsonContent.Create(
                new CreateInvitationHttpRequest
                {
                    OrganizationId = organizationId,
                    EmailAddress = emailAddress,
                    Role = role,
                    InviterUserId = inviterUserId,
                    State = FakeInvitationState.Pending,
                },
                options: TestJsonSerializerOptions.Default
            ),
        };

        return SendAsync(request, scenarioId, cancellationToken);
    }

    private async Task ConfigureFaultAsync(
        FailOperationHttpRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            "/faults",
            request,
            TestJsonSerializerOptions.Default,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    private async Task<FakeInvitation[]> ReadInvitationListAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<InvitationListHttpResponse>(
            TestJsonSerializerOptions.Default,
            cancellationToken
        );

        return result?.Invitations ?? throw new InvalidOperationException(
            "The fake WorkOS service returned an empty invitation list response."
        );
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        string scenarioId,
        CancellationToken cancellationToken)
    {
        using (request)
        {
            request.Headers.Add(TestScenarioDefaults.ScenarioIdHeaderName, scenarioId);

            return await client.SendAsync(request, cancellationToken);
        }
    }

    private async Task<string> PostAndReadIdAsync<TRequest>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var response = await PostAndReadAsync<TRequest, IdentifierHttpResponse>(
            requestUri,
            request,
            cancellationToken
        );

        return response.Id;
    }

    private async Task<TResponse> PostAndReadAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(
            requestUri,
            request,
            TestJsonSerializerOptions.Default,
            cancellationToken
        );
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
            TestJsonSerializerOptions.Default,
            cancellationToken
        )
            ?? throw new InvalidOperationException(
                "The fake WorkOS service returned an empty response."
            );
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

        public required string UserEmailAddress { get; init; }

        public string? UserFirstName { get; init; }

        public string? UserLastName { get; init; }

        public string? UserDisplayName { get; init; }

        public string? UserProfilePictureUrl { get; init; }

        public required string OrganizationName { get; init; }

        public required string OrganizationExternalId { get; init; }

        public required FakeMembershipRole Role { get; init; }
    }

    private sealed record CreateUserHttpRequest
    {
        public required string Id { get; init; }

        public required string EmailAddress { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? DisplayName { get; init; }

        public string? ProfilePictureUrl { get; init; }
    }

    private sealed record UpdateUserHttpRequest
    {
        public required string DisplayName { get; init; }

        public string? ProfilePictureUrl { get; init; }
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

        public required FakeMembershipRole Role { get; init; }

        public required FakeMembershipStatus Status { get; init; }
    }

    private sealed record UpdateMembershipRoleHttpRequest
    {
        public required FakeMembershipRole Role { get; init; }
    }

    private sealed record CreateInvitationHttpRequest
    {
        public string? Id { get; init; }

        public required string OrganizationId { get; init; }

        public required string EmailAddress { get; init; }

        public required FakeMembershipRole Role { get; init; }

        public required string InviterUserId { get; init; }

        public required FakeInvitationState State { get; init; }
    }

    private sealed record FailOperationHttpRequest
    {
        public required string ScenarioId { get; init; }

        public required FakeWorkOsOperation Operation { get; init; }

        public string? RecipientEmailAddress { get; init; }
    }

    private sealed record IdentifierHttpResponse
    {
        public required string Id { get; init; }
    }

    private sealed record ActiveCrewMemberListHttpResponse
    {
        public required FakeActiveCrewMember[] Members { get; init; }
    }

    private sealed record InvitationListHttpResponse
    {
        public required FakeInvitation[] Invitations { get; init; }
    }
}

internal sealed record FakeWorkOsSetup
{
    public required string UserId { get; init; }

    public required string OrganizationId { get; init; }

    public required string MembershipId { get; init; }
}

internal sealed record FakeActiveCrewMember
{
    public required string WorkOsUserId { get; init; }

    public required string DisplayName { get; init; }

    public required string EmailAddress { get; init; }

    public required FakeMembershipRole Role { get; init; }

    public string? ProfilePictureUrl { get; init; }
}

internal sealed record FakeInvitation
{
    public required string Id { get; init; }

    public required string OrganizationId { get; init; }

    public required string EmailAddress { get; init; }

    public required FakeInvitationState State { get; init; }

    public required FakeMembershipRole Role { get; init; }

    public required string InviterUserId { get; init; }
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

internal enum FakeInvitationState
{
    Pending,
    Accepted,
    Revoked,
    Expired,
}

public enum FakeWorkOsOperation
{
    FindOrganizationByExternalId,
    CreateOrganization,
    DeleteOrganization,
    FindActiveMembership,
    CreateMembership,
    UpdateMembership,
    DeactivateMembership,
    ListActiveMembers,
    ListPendingInvitations,
    SendInvitation,
}
