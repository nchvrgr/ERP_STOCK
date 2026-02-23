using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Servidor.Aplicacion.Contratos;
using Servidor.Infraestructura.Persistence;
using Servidor.Infraestructura.Repositories;
using Servidor.Infraestructura.Security;
using Servidor.Infraestructura.Services;
using Servidor.Infraestructura.Adapters.Pdf;
using Servidor.Infraestructura.Adapters.Fiscal;

namespace Servidor.Infraestructura;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");
        var databaseUrl = configuration["DATABASE_URL"]
            ?? configuration["DATABASE_PUBLIC_URL"]
            ?? Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL");

        var shouldResolveFromDatabaseUrl = string.IsNullOrWhiteSpace(connectionString)
            || string.Equals(connectionString, "DATABASE_URL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(connectionString, "DATABASE_PUBLIC_URL", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase));

        if (shouldResolveFromDatabaseUrl && !string.IsNullOrWhiteSpace(databaseUrl))
        {
            connectionString = MapDatabaseUrl(databaseUrl) ?? connectionString;
        }
        else if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = MapDatabaseUrl(connectionString) ?? connectionString;
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'Default' is not configured.");
        }

        services.AddDbContext<PosDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IRepositorioAutenticacion, RepositorioAutenticacion>();
        services.AddScoped<IRepositorioRolesUsuario, RepositorioRolesUsuario>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IRepositorioProductos, RepositorioProductos>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockMovementRepository, StockMovementRepository>();
        services.AddScoped<ICajaRepository, CajaRepository>();
        services.AddScoped<IVentaRepository, VentaRepository>();
        services.AddScoped<IProveedorRepository, ProveedorRepository>();
        services.AddScoped<IOrdenCompraRepository, OrdenCompraRepository>();
        services.AddScoped<IDocumentoCompraRepository, DocumentoCompraRepository>();
        services.AddScoped<IPreRecepcionRepository, PreRecepcionRepository>();
        services.AddScoped<IDocumentParser, Adapters.JsonDocumentParser>();
        services.AddScoped<IRecepcionRepository, RecepcionRepository>();
        services.AddScoped<IListaPrecioRepository, ListaPrecioRepository>();
        services.AddScoped<IDevolucionRepository, DevolucionRepository>();
        services.AddScoped<IEtiquetaPdfGenerator, EtiquetaPdfGenerator>();
        services.AddScoped<ICodigoBarraPdfGenerator, CodigoBarraPdfGenerator>();
        services.AddScoped<IRemitoPdfGenerator, RemitoPdfGenerator>();
        services.AddScoped<IAuditLogQueryRepository, AuditLogQueryRepository>();
        services.AddScoped<IReportesRepository, ReportesRepository>();
        services.AddScoped<IFiscalProvider, DummyFiscalProvider>();
        services.AddScoped<IComprobanteRepository, ComprobanteRepository>();
        services.AddScoped<ICategoriaPrecioRepository, CategoriaPrecioRepository>();
        services.AddScoped<IEmpresaDatosRepository, EmpresaDatosRepository>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        return services;
    }

    private static string? MapDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return null;
        }

        if (!databaseUrl.StartsWith("postgres", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty;
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port.ToString() : "5432";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Ssl Mode=Require;Trust Server Certificate=true";
    }
}



