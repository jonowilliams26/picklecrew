using Microsoft.EntityFrameworkCore;

namespace PickleCrew.Api.Data.Database;

internal static class DatabaseInitialization
{
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PickleCrewDbContext>();

        await dbContext.Database.MigrateAsync();

        return app;
    }
}
