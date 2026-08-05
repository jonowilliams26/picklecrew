using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace PickleCrew.IntegrationTests.ErrorHandling;

public sealed class ProblemDetailsTests(AppHostFixture fixture) : IClassFixture<AppHostFixture>
{
    [Fact]
    public async Task Request_WhenEndpointDoesNotExist_ShouldReturnProblemDetailsWithTraceId()
    {
        using var httpResponse = await fixture.ApiClient.GetAsync("/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);

        var response = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(response);
        var traceId = Assert.IsType<JsonElement>(response.Extensions["traceId"]);
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
