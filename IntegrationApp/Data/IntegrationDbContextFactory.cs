using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IntegrationApp.Data;

/// <summary>
/// Factory para EF Core CLI (migrations). Solo se usa en design-time.
/// </summary>
public class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=IntegrationAppDb;Trusted_Connection=True;MultipleActiveResultSets=True;");

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
