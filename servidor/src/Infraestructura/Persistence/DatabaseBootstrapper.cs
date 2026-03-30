using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Servidor.Dominio.Entities;

namespace Servidor.Infraestructura.Persistence;

public static class DatabaseBootstrapper
{
    private static readonly Guid EmpresaDatosId = Guid.Parse("77f7b2b4-4a3d-4f84-9ea5-1a7cb129d001");

    public static async Task InitializeAsync(PosDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        await EnsureCajaSchemaAsync(dbContext, cancellationToken);
        await EnsureEmpresaDatosSchemaAsync(dbContext, cancellationToken);

        await SeedCoreDataAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureCajaSchemaAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(dbContext, "cajas", "DefaultMontoInicial", cancellationToken))
        {
            return;
        }

        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE cajas ADD COLUMN DefaultMontoInicial NUMERIC NOT NULL DEFAULT 0;",
                cancellationToken);
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE cajas ADD COLUMN \"DefaultMontoInicial\" numeric(18,2) NOT NULL DEFAULT 0;",
            cancellationToken);
    }

    private static async Task EnsureEmpresaDatosSchemaAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(dbContext, "empresa_datos", "MedioPagoHabitual", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN MedioPagoHabitual TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN \"MedioPagoHabitual\" character varying(100) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "empresa_datos", "MediosPagoJson", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN MediosPagoJson TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN \"MediosPagoJson\" character varying(4000) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "empresa_datos", "RemitoSecuencia", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN RemitoSecuencia INTEGER NOT NULL DEFAULT 0;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE empresa_datos ADD COLUMN \"RemitoSecuencia\" integer NOT NULL DEFAULT 0;",
                    cancellationToken);
            }
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        PosDbContext dbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            if (dbContext.Database.IsSqlite())
            {
                command.CommandText = $"PRAGMA table_info('{tableName}')";
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            command.CommandText = """
                SELECT 1
                FROM information_schema.columns
                WHERE table_name = @tableName
                  AND column_name = @columnName
                LIMIT 1
                """;

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "@tableName";
            tableParam.Value = tableName;
            command.Parameters.Add(tableParam);

            var columnParam = command.CreateParameter();
            columnParam.ParameterName = "@columnName";
            columnParam.Value = columnName;
            command.Parameters.Add(columnParam);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null && result != DBNull.Value;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task SeedCoreDataAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Tenants.AnyAsync(cancellationToken))
        {
            await UpgradeDesktopSeedDataAsync(dbContext, cancellationToken);
            return;
        }

        dbContext.Tenants.Add(SeedData.Tenant);
        dbContext.Sucursales.Add(SeedData.Sucursal);
        dbContext.Usuarios.Add(SeedData.AdminUser);
        dbContext.Roles.AddRange(SeedData.Roles);
        dbContext.Permisos.AddRange(SeedData.Permissions);
        dbContext.UsuarioRoles.AddRange(SeedData.UserRoles);
        dbContext.RolPermisos.AddRange(SeedData.RolePermissions);
        dbContext.EmpresaDatos.Add(new EmpresaDatos(
            EmpresaDatosId,
            SeedData.TenantId,
            "Mi Empresa",
            null,
            null,
            null,
            null,
            null,
            null,
            "EFECTIVO",
            "[\"EFECTIVO\",\"TARJETA\",\"TRANSFERENCIA\",\"APLICATIVO\",\"OTRO\"]",
            SeedData.SeedTimestamp));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpgradeDesktopSeedDataAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        var admin = await dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);

        if (admin is null)
        {
            return;
        }

        if (!string.Equals(admin.PasswordHash, SeedData.LegacyAdminPasswordHash, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        dbContext.Entry(admin).Property(nameof(User.PasswordHash)).CurrentValue = SeedData.AdminPasswordHash;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
