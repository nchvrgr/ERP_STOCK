using Microsoft.Extensions.DependencyInjection;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.CasosDeUso.Autenticacion;
using Servidor.Aplicacion.CasosDeUso.Salud;
using Servidor.Aplicacion.CasosDeUso.Usuarios;
using Servidor.Aplicacion.CasosDeUso.Productos;
using Servidor.Aplicacion.CasosDeUso.Stock;
using Servidor.Aplicacion.CasosDeUso.Caja;
using Servidor.Aplicacion.CasosDeUso.Ventas;
using Servidor.Aplicacion.CasosDeUso.Proveedores;
using Servidor.Aplicacion.CasosDeUso.Compras;
using Servidor.Aplicacion.CasosDeUso.DocumentosCompra;
using Servidor.Aplicacion.CasosDeUso.PreRecepciones;
using Servidor.Aplicacion.CasosDeUso.ListasPrecio;
using Servidor.Aplicacion.CasosDeUso.Precios;
using Servidor.Aplicacion.CasosDeUso.Devoluciones;
using Servidor.Aplicacion.CasosDeUso.Importaciones;
using Servidor.Aplicacion.CasosDeUso.Etiquetas;
using Servidor.Aplicacion.CasosDeUso.Auditoria;
using Servidor.Aplicacion.CasosDeUso.Reportes;
using Servidor.Aplicacion.CasosDeUso.Comprobantes;
using Servidor.Aplicacion.CasosDeUso.Precios.Estrategias;
using Servidor.Aplicacion.CasosDeUso.Categorias;
using Servidor.Aplicacion.CasosDeUso.Empresa;

namespace Servidor.Aplicacion.Comun;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IServicioSalud, ServicioSalud>();
        services.AddScoped<ServicioAutenticacion>();
        services.AddScoped<ServicioRolesUsuario>();
        services.AddScoped<ServicioProductos>();
        services.AddScoped<StockService>();
        services.AddScoped<CajaService>();
        services.AddScoped<VentaService>();
        services.AddScoped<ProveedorService>();
        services.AddScoped<OrdenCompraService>();
        services.AddScoped<DocumentoCompraService>();
        services.AddScoped<PreRecepcionService>();
        services.AddScoped<ListaPrecioService>();
        services.AddScoped<DevolucionService>();
        services.AddScoped<ImportacionProductosService>();
        services.AddScoped<EtiquetasService>();
        services.AddScoped<AuditoriaService>();
        services.AddScoped<ReportesService>();
        services.AddScoped<ComprobantesService>();
        services.AddScoped<CategoriaPrecioService>();
        services.AddScoped<EmpresaDatosService>();
        services.AddScoped<IPromoStrategy, PorcCategoriaPromoStrategy>();
        services.AddScoped<IPromoStrategy, DosPorUnoPromoStrategy>();
        services.AddScoped<IPromoStrategy, ComboPromoStrategy>();
        services.AddScoped<PricingService>();
        return services;
    }
}



