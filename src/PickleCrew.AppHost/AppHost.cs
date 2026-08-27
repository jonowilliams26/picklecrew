var builder = DistributedApplication.CreateBuilder(args);

builder.AddParameter("testing", "false", publishValueAsDefault: true);

var isTesting = bool.TryParse(
    builder.Configuration["Parameters:testing"],
    out var testing
) && testing;
var webHost = builder.Configuration["web-host"];
var configuredWebPort = builder.Configuration["web-port"];
var hasWebHost = !string.IsNullOrWhiteSpace(webHost);
var hasWebPort = !string.IsNullOrWhiteSpace(configuredWebPort);

if (hasWebHost != hasWebPort)
{
    throw new DistributedApplicationException(
        "The web-host and web-port arguments must be provided together."
    );
}

var exposeWebToLocalNetwork = hasWebHost && hasWebPort;
var webPort = 0;

if (exposeWebToLocalNetwork)
{
    var webPortIsValid = int.TryParse(configuredWebPort, out webPort)
        && webPort is >= 1 and <= 65535;

    if (!webPortIsValid)
    {
        throw new DistributedApplicationException(
            $"The web-port argument must be between 1 and 65535, but was '{configuredWebPort}'."
        );
    }
}

var workosClientId = builder.AddParameter("workos-client-id");
var workosApiKey = builder.AddParameter("workos-api-key", secret: true);
var workosCookiePassword = builder.AddParameter("workos-cookie-password", secret: true);

var postgres = builder.AddPostgres("postgres");

if (!isTesting)
{
    postgres.WithDataVolume();
}

var database = postgres.AddDatabase("picklecrew");

var api = builder.AddProject<Projects.PickleCrew_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("WorkOS__ClientId", workosClientId)
    .WithEnvironment("WorkOS__ApiKey", workosApiKey)
    .WithHttpHealthCheck("/health", endpointName: "http");

if (isTesting)
{
    var fakeWorkOs = builder.AddProject<Projects.PickleCrew_FakeWorkOs>("fake-workos")
        .WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
        .WithHttpHealthCheck("/health", endpointName: "http");

    api.WithReference(fakeWorkOs)
        .WaitFor(fakeWorkOs)
        .WithEnvironment("DOTNET_ENVIRONMENT", "Testing")
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing");
}

var web = builder.AddViteApp("web", "../PickleCrew.Web")
    .WithPnpm()
    .WithReference(api)
    .WithEnvironment("API_BASE_URL", api.GetEndpoint("http"))
    .WithEnvironment("WORKOS_CLIENT_ID", workosClientId)
    .WithEnvironment("WORKOS_API_KEY", workosApiKey)
    .WithEnvironment("WORKOS_COOKIE_PASSWORD", workosCookiePassword);

if (exposeWebToLocalNetwork)
{
    var webOrigin = new UriBuilder(Uri.UriSchemeHttp, webHost!, webPort)
        .Uri.GetLeftPart(UriPartial.Authority);

    web.WithEndpoint("http", endpoint =>
    {
        endpoint.IsProxied = false;
        endpoint.Port = webPort;
        endpoint.TargetPort = webPort;
        endpoint.TargetHost = "0.0.0.0";
    });
    web.WithEnvironment(
        "WORKOS_REDIRECT_URI",
        $"{webOrigin}/api/auth/callback"
    );
}
else
{
    web.WithEnvironment(
        "WORKOS_REDIRECT_URI",
        $"{web.GetEndpoint("http")}/api/auth/callback"
    );
}

builder.Build().Run();
