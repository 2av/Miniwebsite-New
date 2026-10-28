using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiniWebsite.Domain.Entities;

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
        var removedProfiles = await db.UploadedFiles
            .Where(f => f.Folder == UploadedFileFolder.Profile && f.Status == UploadedFileStatus.Replaced)
            .ExecuteDeleteAsync(ct);
        if (removedProfiles > 0)
            logger.LogInformation("Removed {Count} profile file records whose storage objects were already deleted.", removedProfiles);
        await db.EnsureDbCodesAsync(ct);
        await seeder.SeedAsync(ct);
    }
}
