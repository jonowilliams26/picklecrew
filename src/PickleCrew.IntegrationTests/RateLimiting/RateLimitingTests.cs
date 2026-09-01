using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PickleCrew.Api.RateLimiting;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests.RateLimiting;

[Collection(nameof(AppHostCollection))]
public sealed class RateLimitingTests(AppHostFixture fixture)
{
    [Fact]
    public async Task AuthenticatedUserRateLimit_WhenPermitLimitExceeded_ShouldRejectOnlyLimitedUser()
    {
        var limitedIdentity = TestIdentity.CreateSubjectOnly();
        using var limitedClient = fixture.CreateApiClient(limitedIdentity);
        var requestTasks = Enumerable
            .Range(0, RateLimitingPolicies.AuthenticatedUserPermitLimit + 1)
            .Select(_ => limitedClient.GetAsync("/testing/user-context"))
            .ToArray();
        var responses = await Task.WhenAll(requestTasks);

        try
        {
            Assert.Equal(
                RateLimitingPolicies.AuthenticatedUserPermitLimit,
                responses.Count(response => response.StatusCode == HttpStatusCode.OK)
            );

            var rejectedResponse = Assert.Single(
                responses,
                response => response.StatusCode == HttpStatusCode.TooManyRequests
            );

            Assert.NotNull(rejectedResponse.Headers.RetryAfter?.Delta);

            var problemDetails = await rejectedResponse.Content
                .ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problemDetails);
            Assert.Equal(StatusCodes.Status429TooManyRequests, problemDetails.Status);
            var traceId = Assert.IsType<JsonElement>(problemDetails.Extensions["traceId"]);
            Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));

            var otherIdentity = TestIdentity.CreateSubjectOnly();
            using var otherClient = fixture.CreateApiClient(otherIdentity);
            using var otherResponse = await otherClient.GetAsync("/testing/user-context");

            Assert.Equal(HttpStatusCode.OK, otherResponse.StatusCode);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }
}
