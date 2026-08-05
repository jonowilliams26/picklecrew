using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.EntityFrameworkCore;
using PickleCrew.Api.Data.Database;
using Xunit;

namespace PickleCrew.IntegrationTests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
    private DistributedApplication? application;

    public HttpClient ApiClient { get; private set; } = null!;

    public string DatabaseConnectionString { get; private set; } = null!;

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
                timeout.Token
            );
        appHost.Environment.EnvironmentName = "Testing";

        application = await appHost.BuildAsync(timeout.Token)
            .WaitAsync(DefaultTimeout, timeout.Token);

        await application.StartAsync(timeout.Token)
            .WaitAsync(DefaultTimeout, timeout.Token);
        await application.ResourceNotifications.WaitForResourceHealthyAsync(
            "picklecrew",
            timeout.Token
        ).WaitAsync(DefaultTimeout, timeout.Token);

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

        ApiClient = application.CreateHttpClient("api");
    }

    public async Task DisposeAsync()
    {
        ApiClient.Dispose();

        if (application is not null)
        {
            await application.DisposeAsync();
        }
    }
}
