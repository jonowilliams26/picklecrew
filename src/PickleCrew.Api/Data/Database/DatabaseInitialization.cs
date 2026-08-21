using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Data.Database;

internal static class DatabaseInitialization
{
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<PickleCrewDbContext>();

        await database.Database.MigrateAsync();

        return app;
    }
}
