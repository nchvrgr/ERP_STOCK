using Microsoft.EntityFrameworkCore;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Entities;

namespace Servidor.Infraestructura.Persistence;

public sealed class PosDbContext : DbContext
{
    public PosDbContext(DbContextOptions<PosDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<EmpresaDatos> EmpresaDatos => Set<EmpresaDatos>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<User> Usuarios => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permisos => Set<Permission>();
    public DbSet<UserRole> UsuarioRoles => Set<UserRole>();
    public DbSet<RolePermission> RolPermisos => Set<RolePermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<ProductoCodigo> ProductoCodigos => Set<ProductoCodigo>();
    public DbSet<ProductoProveedor> ProductoProveedores => Set<ProductoProveedor>();
    public DbSet<ProductoStockConfig> ProductoStockConfigs => Set<ProductoStockConfig>();
    public DbSet<StockSaldo> StockSaldos => Set<StockSaldo>();
    public DbSet<StockMovimiento> StockMovimientos => Set<StockMovimiento>();
    public DbSet<StockMovimientoItem> StockMovimientoItems => Set<StockMovimientoItem>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraItem> OrdenCompraItems => Set<OrdenCompraItem>();
    public DbSet<DocumentoCompra> DocumentosCompra => Set<DocumentoCompra>();
    public DbSet<DocumentoCompraItem> DocumentoCompraItems => Set<DocumentoCompraItem>();
    public DbSet<PreRecepcion> PreRecepciones => Set<PreRecepcion>();
    public DbSet<PreRecepcionItem> PreRecepcionItems => Set<PreRecepcionItem>();
    public DbSet<Recepcion> Recepciones => Set<Recepcion>();
    public DbSet<RecepcionItem> RecepcionItems => Set<RecepcionItem>();
    public DbSet<ListaPrecio> ListasPrecio => Set<ListaPrecio>();
    public DbSet<ListaPrecioItem> ListaPrecioItems => Set<ListaPrecioItem>();
    public DbSet<Devolucion> Devoluciones => Set<Devolucion>();
    public DbSet<DevolucionItem> DevolucionItems => Set<DevolucionItem>();
    public DbSet<NotaCreditoInterna> NotasCreditoInterna => Set<NotaCreditoInterna>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<VentaItem> VentaItems => Set<VentaItem>();
    public DbSet<VentaPago> VentaPagos => Set<VentaPago>();
    public DbSet<Caja> Cajas => Set<Caja>();
    public DbSet<CajaSesion> CajaSesiones => Set<CajaSesion>();
    public DbSet<CajaMovimiento> CajaMovimientos => Set<CajaMovimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PosDbContext).Assembly);
        ConfigureProviderSpecificModel(modelBuilder);
    }

    private void ConfigureProviderSpecificModel(ModelBuilder modelBuilder)
    {
        if (!Database.IsSqlite())
        {
            modelBuilder.Entity<Venta>()
                .Property(x => x.Numero)
                .HasDefaultValueSql("nextval('venta_numero_seq')")
                .ValueGeneratedOnAdd();

            return;
        }

        modelBuilder.Entity<Venta>()
            .Property(x => x.Numero)
            .HasDefaultValue(0L)
            .HasDefaultValueSql(null)
            .ValueGeneratedNever();

        modelBuilder.Entity<Caja>()
            .HasIndex(x => new { x.TenantId, x.SucursalId, x.Numero })
            .IsUnique()
            .HasFilter(null);

        modelBuilder.Entity<CajaSesion>()
            .Property(x => x.ArqueoJson)
            .HasColumnType("TEXT");

        modelBuilder.Entity<CajaSesion>()
            .HasIndex(x => new { x.CajaId, x.Estado })
            .IsUnique()
            .HasFilter($"{nameof(CajaSesion.Estado)} = {(int)CajaSesionEstado.Abierta}");

        modelBuilder.Entity<ProductoProveedor>()
            .HasIndex(x => new { x.TenantId, x.ProductoId })
            .IsUnique()
            .HasFilter($"{nameof(ProductoProveedor.EsPrincipal)} = 1");
    }
}

