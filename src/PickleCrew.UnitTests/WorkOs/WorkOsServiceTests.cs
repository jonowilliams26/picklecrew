using System.Net;
using System.Text.Json;
using PickleCrew.Api.WorkOs;
using WorkOS;
using Xunit;

namespace PickleCrew.UnitTests.WorkOs;

public sealed class WorkOsServiceTests
{
    [Fact]
    public async Task SendCrewInvitationAsync_WhenRequestSucceeds_ShouldSendMemberInvitationWithDefaultsOmitted()
    {
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.Created);
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
        var handler = new RecordingHttpMessageHandler(HttpStatusCode.InternalServerError);
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

    private sealed class RecordingHttpMessageHandler(HttpStatusCode statusCode)
        : HttpMessageHandler
    {
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

            var responseBody = statusCode == HttpStatusCode.Created
                ? """
                  {
                    "id": "invitation_123",
                    "object": "invitation",
                    "email": "friend@example.com",
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
                  """
                : "{\"message\":\"WorkOS failed.\"}";

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody),
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
