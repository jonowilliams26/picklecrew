using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

[Collection(nameof(AppHostCollection))]
public sealed class FakeWorkOsEndpointsTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Setup_WhenRequestIsValid_ShouldCreateOrganizationUserAndMembership()
    {
        var userId = $"user_{Guid.NewGuid():N}";
        var externalId = $"crew_{Guid.NewGuid():N}";
        using var client = fixture.CreateFakeWorkOsClient();

        using var setupResponse = await client.PostAsJsonAsync(
            "/setup",
            new
            {
                userId,
                organizationName = "Kitchen Legends",
                organizationExternalId = externalId,
                role = "member",
            }
        );

        Assert.Equal(HttpStatusCode.Created, setupResponse.StatusCode);

        var setup = await setupResponse.Content.ReadFromJsonAsync<SetupHttpResponse>();
        Assert.NotNull(setup);
        Assert.Equal(userId, setup.UserId);
        Assert.StartsWith("org_", setup.OrganizationId);
        Assert.StartsWith("om_", setup.MembershipId);

        using var organizationResponse = await client.GetAsync(
            $"/organizations/by-external-id/{externalId}"
        );
        using var membershipResponse = await client.GetAsync(
            $"/organizations/{setup.OrganizationId}/active-memberships/{userId}"
        );

        Assert.Equal(HttpStatusCode.OK, organizationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, membershipResponse.StatusCode);

        var organization = await organizationResponse.Content
            .ReadFromJsonAsync<IdentifierHttpResponse>();
        var membership = await membershipResponse.Content
            .ReadFromJsonAsync<IdentifierHttpResponse>();

        Assert.NotNull(organization);
        Assert.NotNull(membership);
        Assert.Equal(setup.OrganizationId, organization.Id);
        Assert.Equal(setup.MembershipId, membership.Id);
    }

    private sealed record SetupHttpResponse
    {
        public required string UserId { get; init; }

        public required string OrganizationId { get; init; }

        public required string MembershipId { get; init; }
    }

    private sealed record IdentifierHttpResponse
    {
        public required string Id { get; init; }
    }
}
