using System.Net;
using System.Net.Http.Json;
using PickleCrew.Api.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

[Collection(nameof(AppHostCollection))]
public sealed class TestAuthenticationTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Authenticate_WhenOrganizationAndRoleHeadersAreSupplied_ShouldPropagateClaims()
    {
        var identity = TestIdentity.Create(role: "admin");
        using var client = fixture.CreateApiClient(identity);

        using var response = await client.GetAsync("/testing/work-os-user-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var context = await response.Content
            .ReadFromJsonAsync<TestWorkOsUserContextHttpResponse>();

        Assert.NotNull(context);
        Assert.Equal(identity.UserId, context.UserId);
        Assert.Equal(identity.OrganizationId, context.OrganizationId);
        Assert.Equal(identity.Role, context.Role);
    }
}
