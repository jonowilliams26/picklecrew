using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace PickleCrew.IntegrationTests.Onboarding;

[Collection(nameof(AppHostCollection))]
public sealed class CreateCrewEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task CreateCrew_WhenUserIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        using var response = await fixture.ApiClient.PostAsJsonAsync(
            "/onboarding/crew",
            new { name = "Kitchen Legends" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
