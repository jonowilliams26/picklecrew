using System.Net;
using System.Text.Json;
using PickleCrew.Api.Services;
using PickleCrew.Api.WorkOs;
using WorkOS;
using Xunit;

namespace PickleCrew.UnitTests.WorkOs;

public sealed class WorkOsServiceTests
{
    [Fact]
    public async Task SendCrewInvitationAsync_WhenRequestSucceeds_ShouldSendMemberInvitationWithDefaultsOmitted()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.Created, CreateInvitationResponse("friend@example.com"))
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await service.SendCrewInvitationAsync(
            "org_123",
            "friend@example.com",
            "user_123",
            CancellationToken.None
        );

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/user_management/invitations", request.RequestUri.AbsolutePath);

        using var body = JsonDocument.Parse(request.Body);
        var root = body.RootElement;

        Assert.Equal("friend@example.com", root.GetProperty("email").GetString());
        Assert.Equal("org_123", root.GetProperty("organization_id").GetString());
        Assert.Equal("member", root.GetProperty("role_slug").GetString());
        Assert.Equal("user_123", root.GetProperty("inviter_user_id").GetString());
        Assert.False(root.TryGetProperty("expires_in_days", out _));
        Assert.False(root.TryGetProperty("locale", out _));
    }

    [Fact]
    public async Task SendCrewInvitationAsync_WhenWorkOsReturnsServerError_ShouldNotRetryMutation()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.InternalServerError, "{\"message\":\"WorkOS failed.\"}")
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await Assert.ThrowsAsync<ServerException>(
            () => service.SendCrewInvitationAsync(
                "org_123",
                "friend@example.com",
                "user_123",
                CancellationToken.None
            )
        );

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UpdateOrganizationNameAsync_WhenRequestSucceeds_ShouldUpdateName()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.OK, CreateOrganizationResponse("Dink Dynasty"))
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await service.UpdateOrganizationNameAsync(
            "org_123",
            "Dink Dynasty",
            CancellationToken.None
        );

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal("/organizations/org_123", request.RequestUri.AbsolutePath);

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("Dink Dynasty", body.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateOrganizationNameAsync_WhenWorkOsReturnsServerError_ShouldNotRetryMutation()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.InternalServerError, "{\"message\":\"WorkOS failed.\"}")
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await Assert.ThrowsAsync<ServerException>(
            () => service.UpdateOrganizationNameAsync(
                "org_123",
                "Dink Dynasty",
                CancellationToken.None
            )
        );

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task UpdateOrganizationMembershipRoleAsync_WhenMembershipIsActive_ShouldUpdateRole()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_123",
                    "member@example.com"
                )
            ),
            (
                HttpStatusCode.OK,
                CreateMembershipResponse(
                    "user_123",
                    "member@example.com",
                    "active",
                    "admin"
                )
            )
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var wasUpdated = await service.UpdateOrganizationMembershipRoleAsync(
            "org_123",
            "user_123",
            "admin",
            CancellationToken.None
        );

        Assert.True(wasUpdated);
        Assert.Equal(2, handler.Requests.Count);
        var request = handler.Requests[1];
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.Equal(
            "/user_management/organization_memberships/membership_user_123",
            request.RequestUri.AbsolutePath
        );

        using var body = JsonDocument.Parse(request.Body);
        Assert.Equal("admin", body.RootElement.GetProperty("role_slug").GetString());
    }

    [Fact]
    public async Task UpdateOrganizationMembershipRoleAsync_WhenWorkOsReturnsServerError_ShouldNotRetryMutation()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_123",
                    "member@example.com"
                )
            ),
            (HttpStatusCode.InternalServerError, "{\"message\":\"WorkOS failed.\"}")
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await Assert.ThrowsAsync<ServerException>(
            () => service.UpdateOrganizationMembershipRoleAsync(
                "org_123",
                "user_123",
                "admin",
                CancellationToken.None
            )
        );

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task UpdateOrganizationMembershipRoleAsync_WhenMembershipIsNotActive_ShouldReturnFalse()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.OK, CreateEmptyMembershipListResponse())
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var wasUpdated = await service.UpdateOrganizationMembershipRoleAsync(
            "org_123",
            "user_123",
            "admin",
            CancellationToken.None
        );

        Assert.False(wasUpdated);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task DeactivateOrganizationMembershipAsync_WhenMembershipIsActive_ShouldDeactivateMembership()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_123",
                    "member@example.com"
                )
            ),
            (
                HttpStatusCode.OK,
                CreateMembershipResponse(
                    "user_123",
                    "member@example.com",
                    "inactive"
                )
            )
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var wasDeactivated = await service.DeactivateOrganizationMembershipAsync(
            "org_123",
            "user_123",
            CancellationToken.None
        );

        Assert.True(wasDeactivated);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(HttpMethod.Put, handler.Requests[1].Method);
        Assert.Equal(
            "/user_management/organization_memberships/membership_user_123/deactivate",
            handler.Requests[1].RequestUri.AbsolutePath
        );
    }

    [Fact]
    public async Task DeactivateOrganizationMembershipAsync_WhenWorkOsReturnsServerError_ShouldNotRetryMutation()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_123",
                    "member@example.com"
                )
            ),
            (HttpStatusCode.InternalServerError, "{\"message\":\"WorkOS failed.\"}")
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        await Assert.ThrowsAsync<ServerException>(
            () => service.DeactivateOrganizationMembershipAsync(
                "org_123",
                "user_123",
                CancellationToken.None
            )
        );

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task DeactivateOrganizationMembershipAsync_WhenMembershipIsNotActive_ShouldReturnFalse()
    {
        var handler = new RecordingHttpMessageHandler(
            (HttpStatusCode.OK, CreateEmptyMembershipListResponse())
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var wasDeactivated = await service.DeactivateOrganizationMembershipAsync(
            "org_123",
            "user_123",
            CancellationToken.None
        );

        Assert.False(wasDeactivated);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ListActiveCrewMembersAsync_WhenResultsSpanPages_ShouldReturnIdsNamesPicturesAndEmailFallback()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_one",
                    "member-one@example.com",
                    "Ada",
                    "Lovelace",
                    "Ada L.",
                    "membership_cursor_one",
                    "https://images.example.test/ada.png",
                    "admin"
                )
            ),
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_two",
                    "member-two@example.com",
                    "Grace",
                    "Hopper",
                    after: "membership_cursor_two"
                )
            ),
            (
                HttpStatusCode.OK,
                CreateMembershipListResponse(
                    "user_three",
                    "member-three@example.com"
                )
            )
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var members = await service.ListActiveCrewMembersAsync(
            "org_123",
            CancellationToken.None
        );

        Assert.Collection(
            members,
            member => AssertActiveMember(
                member,
                "user_one",
                "Ada L.",
                "member-one@example.com",
                "admin",
                "https://images.example.test/ada.png"
            ),
            member => AssertActiveMember(
                member,
                "user_two",
                "Grace Hopper",
                "member-two@example.com"
            ),
            member => AssertActiveMember(
                member,
                "user_three",
                "member-three@example.com",
                "member-three@example.com"
            )
        );
        Assert.Equal(3, handler.Requests.Count);
        Assert.All(
            handler.Requests,
            request => Assert.Equal(
                "/user_management/organization_memberships",
                request.RequestUri.AbsolutePath
            )
        );
        Assert.Contains("organization_id=org_123", handler.Requests[0].RequestUri.Query);
        Assert.Contains("statuses[]=active", handler.Requests[0].RequestUri.Query);
        Assert.Contains("after=membership_cursor_one", handler.Requests[1].RequestUri.Query);
        Assert.Contains("after=membership_cursor_two", handler.Requests[2].RequestUri.Query);
    }

    [Fact]
    public async Task ListPendingCrewInvitationsAsync_WhenResultsSpanPages_ShouldReturnOnlyPendingInvitations()
    {
        var handler = new RecordingHttpMessageHandler(
            (
                HttpStatusCode.OK,
                CreateInvitationListResponse(
                    [
                        ("pending-one@example.com", "pending"),
                        ("accepted@example.com", "accepted"),
                    ],
                    "invitation_cursor"
                )
            ),
            (
                HttpStatusCode.OK,
                CreateInvitationListResponse(
                    [
                        ("revoked@example.com", "revoked"),
                        ("pending-two@example.com", "pending"),
                    ]
                )
            )
        );
        using var client = CreateClient(handler);
        var service = new WorkOsService(client);

        var invitations = await service.ListPendingCrewInvitationsAsync(
            "org_123",
            CancellationToken.None
        );

        Assert.Collection(
            invitations,
            invitation =>
            {
                Assert.StartsWith("invitation_", invitation.WorkOsInvitationId);
                Assert.Equal("pending-one@example.com", invitation.EmailAddress);
            },
            invitation =>
            {
                Assert.StartsWith("invitation_", invitation.WorkOsInvitationId);
                Assert.Equal("pending-two@example.com", invitation.EmailAddress);
            }
        );
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(
            handler.Requests,
            request => Assert.Equal(
                "/user_management/invitations",
                request.RequestUri.AbsolutePath
            )
        );
        Assert.Contains("organization_id=org_123", handler.Requests[0].RequestUri.Query);
        Assert.Contains("after=invitation_cursor", handler.Requests[1].RequestUri.Query);
    }

    private static WorkOSClient CreateClient(HttpMessageHandler handler)
    {
        return new WorkOSClient(new WorkOSOptions
        {
            ApiKey = "sk_test",
            ApiBaseURL = "https://workos.test",
            HttpClient = new HttpClient(handler),
            MaxRetries = 2,
        });
    }

    private static void AssertActiveMember(
        ActiveCrewMember member,
        string workOsUserId,
        string displayName,
        string emailAddress,
        string roleSlug = "member",
        string? profilePictureUrl = null)
    {
        Assert.Equal(workOsUserId, member.WorkOsUserId);
        Assert.Equal(displayName, member.DisplayName);
        Assert.Equal(emailAddress, member.EmailAddress);
        Assert.Equal(roleSlug, member.RoleSlug);
        Assert.Equal(profilePictureUrl, member.ProfilePictureUrl);
    }

    private static string CreateOrganizationResponse(string name)
    {
        return JsonSerializer.Serialize(new
        {
            id = "org_123",
            name,
            @object = "organization",
            domains = Array.Empty<object>(),
            external_id = "crew_123",
            metadata = new Dictionary<string, string>(),
            created_at = "2026-08-14T00:00:00Z",
            updated_at = "2026-08-14T00:00:00Z",
        });
    }

    private static string CreateMembershipListResponse(
        string userId,
        string emailAddress,
        string? firstName = null,
        string? lastName = null,
        string? name = null,
        string? after = null,
        string? profilePictureUrl = null,
        string roleSlug = "member")
    {
        return JsonSerializer.Serialize(new
        {
            list_metadata = new
            {
                before = (string?)null,
                after,
            },
            data = new[]
            {
                new
                {
                    id = $"membership_{userId}",
                    user_id = userId,
                    organization_id = "org_123",
                    status = "active",
                    role = new
                    {
                        slug = roleSlug,
                    },
                    user = new
                    {
                        id = userId,
                        email = emailAddress,
                        first_name = firstName,
                        last_name = lastName,
                        name,
                        profile_picture_url = profilePictureUrl,
                    },
                },
            },
        });
    }

    private static string CreateEmptyMembershipListResponse()
    {
        return JsonSerializer.Serialize(new
        {
            list_metadata = new
            {
                before = (string?)null,
                after = (string?)null,
            },
            data = Array.Empty<object>(),
        });
    }

    private static string CreateMembershipResponse(
        string userId,
        string emailAddress,
        string status,
        string roleSlug = "member")
    {
        return JsonSerializer.Serialize(new
        {
            id = $"membership_{userId}",
            user_id = userId,
            organization_id = "org_123",
            status,
            role = new
            {
                slug = roleSlug,
            },
            user = new
            {
                id = userId,
                email = emailAddress,
            },
        });
    }

    private static string CreateInvitationListResponse(
        (string EmailAddress, string State)[] invitations,
        string? after = null)
    {
        var data = invitations.Select(invitation => new
        {
            id = $"invitation_{Guid.NewGuid():N}",
            email = invitation.EmailAddress,
            state = invitation.State,
        });

        return JsonSerializer.Serialize(new
        {
            list_metadata = new
            {
                before = (string?)null,
                after,
            },
            data,
        });
    }

    private static string CreateInvitationResponse(string emailAddress)
    {
        return $$"""
            {
              "id": "invitation_123",
              "object": "invitation",
              "email": "{{emailAddress}}",
              "state": "pending",
              "accepted_at": null,
              "accepted_user_id": null,
              "revoked_at": null,
              "organization_id": "org_123",
              "inviter_user_id": "user_123",
              "role_slug": "member",
              "token": "token_123",
              "accept_invitation_url": "https://workos.test/invitations/token_123",
              "created_at": "2026-08-14T00:00:00Z",
              "updated_at": "2026-08-14T00:00:00Z",
              "expires_at": "2026-08-21T00:00:00Z"
            }
            """;
    }

    private sealed class RecordingHttpMessageHandler(
        params (HttpStatusCode StatusCode, string Body)[] responses)
        : HttpMessageHandler
    {
        private int responseIndex;

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            Requests.Add(new RecordedRequest
            {
                Method = request.Method,
                RequestUri = request.RequestUri
                    ?? throw new InvalidOperationException("Request URI is required."),
                Body = body,
            });

            if (responseIndex >= responses.Length)
            {
                throw new InvalidOperationException("No response was configured for the request.");
            }

            var response = responses[responseIndex];
            responseIndex++;

            return new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(response.Body),
            };
        }
    }

    private sealed record RecordedRequest
    {
        public required HttpMethod Method { get; init; }

        public required Uri RequestUri { get; init; }

        public required string Body { get; init; }
    }
}
