using Microsoft.EntityFrameworkCore;
using Servidor.Infraestructura.Persistence;
using Xunit;

namespace Servidor.Pruebas;

public sealed class DatabaseSeedTests
{
    [Fact]
    public async Task Migrates_and_seeds_demo_tenant()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
            ?? "Host=localhost;Port=5433;Database=posdb;Username=pos;Password=pospass";

        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new PosDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var tenant = await context.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == SeedData.TenantId);

        Assert.NotNull(tenant);
        Assert.Equal("Demo", tenant!.Name);
    }
}

