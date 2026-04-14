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
        await EnsureProductoComboSchemaAsync(dbContext, cancellationToken);
        await EnsureVentaFacturaSchemaAsync(dbContext, cancellationToken);
        await EnsureStockMovimientoFacturaSchemaAsync(dbContext, cancellationToken);

        await SeedCoreDataAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureProductoComboSchemaAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(dbContext, "productos", "IsCombo", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE productos ADD COLUMN IsCombo INTEGER NOT NULL DEFAULT 0;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE productos ADD COLUMN \"IsCombo\" boolean NOT NULL DEFAULT FALSE;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "productos", "ComboItemsJson", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE productos ADD COLUMN ComboItemsJson TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE productos ADD COLUMN \"ComboItemsJson\" character varying(4000) NULL;",
                    cancellationToken);
            }
        }
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

    private static async Task EnsureVentaFacturaSchemaAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(dbContext, "ventas", "tipo_factura", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN tipo_factura TEXT NOT NULL DEFAULT 'B';",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN \"tipo_factura\" character varying(5) NOT NULL DEFAULT 'B';",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "ventas", "cliente_nombre", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN cliente_nombre TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN \"cliente_nombre\" character varying(160) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "ventas", "cliente_cuit", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN cliente_cuit TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN \"cliente_cuit\" character varying(32) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "ventas", "cliente_direccion", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN cliente_direccion TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN \"cliente_direccion\" character varying(240) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "ventas", "cliente_telefono", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN cliente_telefono TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE ventas ADD COLUMN \"cliente_telefono\" character varying(40) NULL;",
                    cancellationToken);
            }
        }
    }

    private static async Task EnsureStockMovimientoFacturaSchemaAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaFacturada", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaFacturada INTEGER NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaFacturada\" boolean NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaTipoFactura", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaTipoFactura TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaTipoFactura\" character varying(5) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaClienteNombre", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaClienteNombre TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaClienteNombre\" character varying(160) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaClienteCuit", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaClienteCuit TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaClienteCuit\" character varying(32) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaClienteDireccion", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaClienteDireccion TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaClienteDireccion\" character varying(240) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaClienteTelefono", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaClienteTelefono TEXT NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaClienteTelefono\" character varying(40) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "VentaTotalNeto", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN VentaTotalNeto NUMERIC NULL;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"VentaTotalNeto\" numeric(18,4) NULL;",
                    cancellationToken);
            }
        }

        if (!await ColumnExistsAsync(dbContext, "stock_movimientos", "FacturaPendiente", cancellationToken))
        {
            if (dbContext.Database.IsSqlite())
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN FacturaPendiente INTEGER NOT NULL DEFAULT 0;",
                    cancellationToken);
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE stock_movimientos ADD COLUMN \"FacturaPendiente\" boolean NOT NULL DEFAULT FALSE;",
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
        dbContext.Usuarios.Add(SeedData.CashierUser);
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
        await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleAdminId, cancellationToken);
        await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleCajeroId, cancellationToken);
    }

    private static async Task UpgradeDesktopSeedDataAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        var admin = await dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Username == "admin", cancellationToken);

        if (admin is null)
        {
            return;
        }

        var mustUpgradeAdminPassword =
            string.Equals(admin.PasswordHash, SeedData.LegacyAdminPasswordHash, StringComparison.OrdinalIgnoreCase)
            || string.Equals(admin.PasswordHash, SeedData.PreviousAdminPasswordHash, StringComparison.OrdinalIgnoreCase);

        if (!mustUpgradeAdminPassword)
        {
            await EnsureCashierUserAsync(dbContext, cancellationToken);
            await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleAdminId, cancellationToken);
            await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleCajeroId, cancellationToken);
            return;
        }

        dbContext.Entry(admin).Property(nameof(User.PasswordHash)).CurrentValue = SeedData.AdminPasswordHash;
        await dbContext.SaveChangesAsync(cancellationToken);
        await EnsureCashierUserAsync(dbContext, cancellationToken);
        await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleAdminId, cancellationToken);
        await EnsureSeedRolePermissionsAsync(dbContext, SeedData.RoleCajeroId, cancellationToken);
    }

    private static async Task EnsureCashierUserAsync(PosDbContext dbContext, CancellationToken cancellationToken)
    {
        var cashier = await dbContext.Usuarios
            .FirstOrDefaultAsync(u => u.Username == "cajero", cancellationToken);

        if (cashier is null)
        {
            cashier = await dbContext.Usuarios
                .FirstOrDefaultAsync(u => u.Username == "empleado", cancellationToken);
        }

        if (cashier is null)
        {
            cashier = SeedData.CashierUser;
            dbContext.Usuarios.Add(cashier);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            cashier.UpdateUsername("cajero");
            cashier.SetActive(true);
            dbContext.Entry(cashier).Property(nameof(User.Username)).CurrentValue = cashier.Username;
            dbContext.Entry(cashier).Property(nameof(User.IsActive)).CurrentValue = cashier.IsActive;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var existingRoles = await dbContext.UsuarioRoles
            .Where(ur => ur.UserId == cashier.Id)
            .ToListAsync(cancellationToken);

        if (existingRoles.Count > 0)
        {
            dbContext.UsuarioRoles.RemoveRange(existingRoles);
        }

        dbContext.UsuarioRoles.Add(new UserRole(
            Guid.NewGuid(),
            cashier.TenantId,
            cashier.Id,
            SeedData.RoleCajeroId,
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSeedRolePermissionsAsync(
        PosDbContext dbContext,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var requiredPermissionIds = SeedData.RolePermissions
            .Where(x => x.RoleId == roleId)
            .Select(x => x.PermissionId)
            .Distinct()
            .ToHashSet();

        var existingPermissionIds = await dbContext.RolPermisos
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(cancellationToken);

        var missingPermissionIds = requiredPermissionIds
            .Where(permissionId => !existingPermissionIds.Contains(permissionId))
            .ToList();

        if (missingPermissionIds.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var permissionId in missingPermissionIds)
        {
            dbContext.RolPermisos.Add(new RolePermission(
                Guid.NewGuid(),
                SeedData.TenantId,
                roleId,
                permissionId,
                now));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
