using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PickleCrew.Api.Crews;
using PickleCrew.Api.Testing;
using PickleCrew.FakeWorkOs;
using Xunit;

namespace PickleCrew.IntegrationTests.Testing;

public sealed class TestingEnvironmentGuardTests
{
    [Fact]
    public async Task TestHeaders_WhenApiIsNotTesting_ShouldNotAuthenticateRequest()
    {
        await using var factory = new WebApplicationFactory<CreateCrewHttpRequest>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting(
                    "ConnectionStrings:picklecrew",
                    "Host=localhost;Database=picklecrew_test;Username=test;Password=test"
                );
                builder.UseSetting("WorkOS:ClientId", "client_test_placeholder");
                builder.UseSetting("WorkOS:ApiKey", "sk_test_placeholder");
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/crews")
        {
            Content = JsonContent.Create(new CreateCrewHttpRequest
            {
                Name = "Kitchen Legends",
            }),
        };
        request.Headers.Add(
            TestAuthenticationDefaults.UserIdHeaderName,
            $"user_test_{Guid.NewGuid():N}"
        );

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task StartFakeWorkOs_WhenEnvironmentIsNotTesting_ShouldRefuseToStart()
    {
        await using var factory = new WebApplicationFactory<CreateOrganizationHttpRequest>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var exception = Assert.Throws<InvalidOperationException>(factory.CreateClient);

        Assert.Contains(
            "can only run in the Testing environment",
            exception.Message,
            StringComparison.Ordinal
        );
    }
}
