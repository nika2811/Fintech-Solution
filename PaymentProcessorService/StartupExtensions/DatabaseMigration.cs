using Microsoft.EntityFrameworkCore;
using PaymentProcessorService.Data;
using Serilog;

namespace PaymentProcessorService.StartupExtensions;

public static class DatabaseMigration
{
    public static async Task MigrateDatabaseAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}