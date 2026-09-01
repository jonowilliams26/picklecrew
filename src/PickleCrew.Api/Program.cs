using Microsoft.EntityFrameworkCore;
using PickleCrew.Api;
using PickleCrew.Api.Data.Database;
using PickleCrew.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDatabase();
builder.AddCaching();
builder.AddWorkOs();
builder.Services.AddTimeProvider();
builder.Services.AddRateLimiting();
builder.Services.AddUserContexts();
builder.Services.AddStatisticsServices(builder.Configuration);
builder.Services.AddRequestLogging(builder.Environment);
builder.Services.AddApiDocumentation();
builder.Services.AddErrorHandling();
builder.Services.AddJsonSerialization();

var app = builder.Build();

app.UseHttpLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRequestTimeouts();
app.UseDevelopmentApiDelay();
app.UseOutputCache();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PickleCrewDbContext>();
    await database.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment() || app.Environment.IsTesting())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapEndpoints();

await app.RunAsync();
