using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Servidor.Infraestructura.Persistence;

public sealed class PosDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
{
    public PosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=127.0.0.1;Port=5433;Database=posdb;Username=pos;Password=pospass";

        var optionsBuilder = new DbContextOptionsBuilder<PosDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PosDbContext(optionsBuilder.Options);
    }
}

