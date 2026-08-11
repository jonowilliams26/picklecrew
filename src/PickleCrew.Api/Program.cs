using PickleCrew.Api;
using PickleCrew.Api.Data.Database;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDatabase();
builder.Services.AddTimeProvider();
builder.Services.AddRequestLogging(builder.Environment);
builder.Services.AddApiDocumentation();
builder.Services.AddErrorHandling();
builder.Services.AddJsonSerialization();

var app = builder.Build();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRequestTimeouts();
app.UseOutputCache();

if (app.Environment.IsDevelopment())
{
    await app.InitializeDatabaseAsync();
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapEndpoints();

await app.RunAsync();
