using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MiniWebsite.Infrastructure.Persistence;

/// <summary>Applies EF migrations then seeds reference data.</summary>
public static class DatabaseBootstrap
{
    public static async Task MigrateAndSeedAsync(
        ApplicationDbContext db,
        IDataSeeder seeder,
        ILogger logger,
        CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("EF Core migrations applied.");
        await seeder.SeedAsync(ct);
    }
}
