using PickleCrew.FakeWorkOs;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        "The fake WorkOS service can only run in the Testing environment."
    );
}

builder.AddServiceDefaults();
builder.Services.AddFakeWorkOs();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapFakeWorkOsEndpoints();

await app.RunAsync();
