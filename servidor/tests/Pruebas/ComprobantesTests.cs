using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Aplicacion.Dtos.Comprobantes;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Aplicacion.Dtos.Stock;
using Servidor.Aplicacion.Dtos.Ventas;
using Servidor.Dominio.Entities;
using Servidor.Dominio.Enums;
using Servidor.Infraestructura.Persistence;
using Xunit;

namespace Servidor.Pruebas;

public sealed class ComprobantesTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public ComprobantesTests(WebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Emitir_comprobante_cambia_estado()
    {
        await _factory.EnsureDatabaseMigratedAsync();

        var client = _factory.CreateClient();
        var token = _factory.CreateTokenWithPermissions(
            PermissionCodes.ProductoEditar,
            PermissionCodes.ProductoVer,
            PermissionCodes.VentaCrear,
            PermissionCodes.VentaConfirmar,
            PermissionCodes.StockAjustar,
            PermissionCodes.CajaMovimiento);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var code = $"CODE-{Guid.NewGuid():N}";
        var productoId = await CrearProductoAsync(client, code);

        var cajaId = await CrearCajaAsync();
        var abrirResponse = await client.PostAsJsonAsync(
            "/api/v1/caja/sesiones/abrir",
            new CajaSesionAbrirDto(cajaId, 0m, "MANANA"));
        Assert.Equal(HttpStatusCode.Created, abrirResponse.StatusCode);

        var ingreso = new StockMovimientoCreateDto(
            "AJUSTE",
            "Ingreso inicial",
            new[]
            {
                new StockMovimientoItemCreateDto(productoId, 5m, true)
            });

        var ingresoResponse = await client.PostAsJsonAsync("/api/v1/stock/ajustes", ingreso);
        Assert.Equal(HttpStatusCode.Created, ingresoResponse.StatusCode);

        var venta = await CrearVentaAsync(client);
        await ConfirmarVentaAsync(client, venta.Id, code);

        var crearComprobante = await client.PostAsync($"/api/v1/comprobantes/borrador-desde-venta/{venta.Id}", null);
        Assert.Equal(HttpStatusCode.Created, crearComprobante.StatusCode);
        var comprobante = await crearComprobante.Content.ReadFromJsonAsync<ComprobanteDto>();
        Assert.NotNull(comprobante);
        Assert.Equal("BORRADOR", comprobante!.Estado);

        var emitirResponse = await client.PostAsync($"/api/v1/comprobantes/{comprobante.Id}/emitir", null);
        Assert.Equal(HttpStatusCode.OK, emitirResponse.StatusCode);
        var emitido = await emitirResponse.Content.ReadFromJsonAsync<ComprobanteDto>();
        Assert.NotNull(emitido);
        Assert.Equal("EMITIDO", emitido!.Estado);
        Assert.False(string.IsNullOrWhiteSpace(emitido.Numero));
    }

    private async Task<Guid> CrearCajaAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        var caja = new Caja(
            Guid.NewGuid(),
            SeedData.TenantId,
            SeedData.SucursalId,
            $"Caja {Guid.NewGuid():N}",
            Random.Shared.Next(1000, 999999).ToString(),
            DateTimeOffset.UtcNow);
        db.Cajas.Add(caja);
        await db.SaveChangesAsync();
        return caja.Id;
    }

    private static async Task<Guid> CrearProductoAsync(HttpClient client, string code)
    {
        var proveedorId = await TestData.CreateProveedorAsync(client);
        var sku = $"SKU-{Guid.NewGuid():N}";
        var createResponse = await client.PostAsJsonAsync("/api/v1/productos", new ProductoCrearDto(
            $"Producto {Guid.NewGuid():N}",
            sku,
            null,
            null,
            proveedorId,
            true));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var product = await createResponse.Content.ReadFromJsonAsync<ProductoDetalleDto>();
        Assert.NotNull(product);

        var addCodeResponse = await client.PostAsJsonAsync(
            $"/api/v1/productos/{product!.Id}/codigos",
            new ProductoCodigoCrearDto(code));

        Assert.Equal(HttpStatusCode.Created, addCodeResponse.StatusCode);
        return product!.Id;
    }

    private static async Task<VentaDto> CrearVentaAsync(HttpClient client)
    {
        var ventaResponse = await client.PostAsync("/api/v1/ventas", null);
        Assert.Equal(HttpStatusCode.Created, ventaResponse.StatusCode);
        var venta = await ventaResponse.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(venta);
        return venta!;
    }

    private static async Task ConfirmarVentaAsync(HttpClient client, Guid ventaId, string code)
    {
        var scan = await client.PostAsJsonAsync(
            $"/api/v1/ventas/{ventaId}/items/scan",
            new VentaScanRequestDto(code));
        Assert.Equal(HttpStatusCode.OK, scan.StatusCode);

        var ventaResponse = await client.GetAsync($"/api/v1/ventas/{ventaId}");
        Assert.Equal(HttpStatusCode.OK, ventaResponse.StatusCode);
        var venta = await ventaResponse.Content.ReadFromJsonAsync<VentaDto>();
        Assert.NotNull(venta);
        var total = venta!.Items.Sum(i => i.Subtotal);

        var confirm = await client.PostAsJsonAsync(
            $"/api/v1/ventas/{ventaId}/confirmar",
            new VentaConfirmRequestDto(new[]
            {
                new VentaPagoRequestDto("EFECTIVO", total)
            }, Facturada: false));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
    }
}



