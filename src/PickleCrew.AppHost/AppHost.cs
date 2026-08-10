var builder = DistributedApplication.CreateBuilder(args);

var workosClientId = builder.AddParameter("workos-client-id");
var workosApiKey = builder.AddParameter("workos-api-key", secret: true);
var workosCookiePassword = builder.AddParameter("workos-cookie-password", secret: true);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("picklecrew");

var api = builder.AddProject<Projects.PickleCrew_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health", endpointName: "http");

var web = builder.AddViteApp("web", "../PickleCrew.Web")
    .WithPnpm()
    .WithReference(api)
    .WithEnvironment("WORKOS_CLIENT_ID", workosClientId)
    .WithEnvironment("WORKOS_API_KEY", workosApiKey)
    .WithEnvironment("WORKOS_COOKIE_PASSWORD", workosCookiePassword);

web.WithEnvironment(
    "WORKOS_REDIRECT_URI",
    $"{web.GetEndpoint("http")}/api/auth/callback"
);

builder.Build().Run();
