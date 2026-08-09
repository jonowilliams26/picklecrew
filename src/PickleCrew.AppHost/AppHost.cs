var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("picklecrew");

var api = builder.AddProject<Projects.PickleCrew_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health", endpointName: "http");

var web = builder.AddViteApp("web", "../PickleCrew.Web")
    .WithPnpm()
    .WithReference(api);

web.WithEnvironment(
    "WORKOS_REDIRECT_URI",
    $"{web.GetEndpoint("http")}/api/auth/callback"
);

builder.Build().Run();
