using ClinicBook.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBook.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Applies any pending migrations and seeds the roles/admin account.
    /// Called only in Development: on a real server migrations are applied as a deliberate
    /// deployment step, not automatically by the running application.
    /// </summary>
    public static async Task MigrateAndSeedDatabaseAsync(this WebApplication app)
    {
        // Services are scoped (one per request), so at startup we create a scope by hand.
        using var scope = app.Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ClinicBookDbContext>();
        await db.Database.MigrateAsync();

        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
}
