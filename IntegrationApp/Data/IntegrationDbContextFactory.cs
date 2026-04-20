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
        optionsBuilder.UseNpgsql(
            Environment.GetEnvironmentVariable("ConnectionStrings__IntegrationDb")
            ?? "Host=localhost;Database=integrationdb;Username=postgres;Password=postgres;");

        return new IntegrationDbContext(optionsBuilder.Options);
    }
}
