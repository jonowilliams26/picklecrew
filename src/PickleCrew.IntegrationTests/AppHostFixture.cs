using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Crews;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Testing;
using PickleCrew.IntegrationTests.Testing;
using Xunit;

namespace PickleCrew.IntegrationTests;

[CollectionDefinition(nameof(AppHostCollection))]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
}

public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);
    private DistributedApplication? application;
    private HttpClient? apiClient;

    public HttpClient ApiClient => apiClient ?? throw new InvalidOperationException(
        "The API client is not available because the AppHost has not finished starting."
    );

    public string CacheConnectionString { get; private set; } = null!;

    public string DatabaseConnectionString { get; private set; } = null!;

    public HttpClient CreateApiClient()
    {
        return application?.CreateHttpClient("api", "http")
            ?? throw new InvalidOperationException(
                "The API client is not available because the AppHost has not finished starting."
            );
    }

    internal HttpClient CreateApiClient(
        TestIdentity identity,
        string? scenarioId = null)
    {
        var client = CreateApiClient();
        client.DefaultRequestHeaders.Add(
            TestAuthenticationDefaults.UserIdHeaderName,
            identity.UserId
        );

        if (identity.OrganizationId is not null)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationDefaults.OrganizationIdHeaderName,
                identity.OrganizationId
            );
        }

        if (identity.Role is not null)
        {
            client.DefaultRequestHeaders.Add(
                TestAuthenticationDefaults.RoleHeaderName,
                identity.Role
            );
        }

        if (scenarioId is not null)
        {
            client.DefaultRequestHeaders.Add(
                TestScenarioDefaults.ScenarioIdHeaderName,
                scenarioId
            );
        }

        return client;
    }

    internal FakeWorkOsClient CreateFakeWorkOsClient()
    {
        var client = application?.CreateHttpClient("fake-workos", "http")
            ?? throw new InvalidOperationException(
                "The fake WorkOS client is not available because the AppHost has not finished starting."
            );

        return new FakeWorkOsClient(client);
    }

    public PickleCrewDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PickleCrewDbContext>()
            .UseNpgsql(DatabaseConnectionString)
            .Options;

        return new PickleCrewDbContext(options);
    }

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(DefaultTimeout);

        var appHost =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.PickleCrew_AppHost>(
                ["--Parameters:testing=true"],
                timeout.Token
            );
        appHost.Configuration["Parameters:workos-client-id"] = "client_test";
        appHost.Configuration["Parameters:workos-api-key"] = "sk_test";
        appHost.Configuration["Parameters:workos-cookie-password"] =
            "test-cookie-password-at-least-32-characters";

        application = await appHost.BuildAsync(timeout.Token)
            .WaitAsync(DefaultTimeout, timeout.Token);

        await application.StartAsync(timeout.Token)
            .WaitAsync(DefaultTimeout, timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            "fake-workos",
            timeout.Token
        ).WaitAsync(DefaultTimeout, timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            "picklecrew",
            timeout.Token
        ).WaitAsync(DefaultTimeout, timeout.Token);

        CacheConnectionString = await application.GetConnectionStringAsync(
            "cache",
            timeout.Token
        ) ?? throw new InvalidOperationException(
            "The cache connection string was not available."
        );
        DatabaseConnectionString = await application.GetConnectionStringAsync(
            "picklecrew",
            timeout.Token
        ) ?? throw new InvalidOperationException(
            "The picklecrew database connection string was not available."
        );

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync(timeout.Token);
        }

        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            "api",
            timeout.Token
        ).WaitAsync(DefaultTimeout, timeout.Token);

        apiClient = application.CreateHttpClient("api", "http");
    }

    public async Task DisposeAsync()
    {
        apiClient?.Dispose();

        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}

internal static class AppHostFixtureExtensions
{
    public static async Task<CreateCrewHttpResponse> CreateCrewAsync(
        this AppHostFixture fixture,
        TestIdentity creator,
        string crewName = "Kitchen Legends")
    {
        using var apiClient = fixture.CreateApiClient(creator);
        using var response = await apiClient.PostAsJsonAsync(
            "/crews",
            new CreateCrewHttpRequest
            {
                RequestId = Guid.NewGuid(),
                Name = crewName,
            }
        );

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CreateCrewHttpResponse>()
            ?? throw new InvalidOperationException(
                "The API returned an empty crew response."
            );
    }
}
