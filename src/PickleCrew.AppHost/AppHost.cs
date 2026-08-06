var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres");
var database = postgres.AddDatabase("picklecrew");

var api = builder.AddProject<Projects.PickleCrew_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health", endpointName: "http");

builder.AddViteApp("web", "../PickleCrew.Web")
    .WithPnpm()
    .WithReference(api);

builder.Build().Run();
