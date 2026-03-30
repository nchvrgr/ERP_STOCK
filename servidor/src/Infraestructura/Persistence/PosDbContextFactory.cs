using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Servidor.Infraestructura.Persistence;

public sealed class PosDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Data Source=pos-local.db";

        var optionsBuilder = new DbContextOptionsBuilder<PosDbContext>();
        if (DependencyInjection.IsSqliteConnectionString(connectionString))
        {
            optionsBuilder.UseSqlite(connectionString);
        }
        else
        {
            optionsBuilder.UseNpgsql(connectionString);
        }

        return new PosDbContext(optionsBuilder.Options);
    }
}

