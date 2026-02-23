using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Servidor.Aplicacion.Dtos.Compras;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Dominio.Enums;
using Xunit;

namespace Servidor.Pruebas;

public sealed class OrdenCompraTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public OrdenCompraTests(WebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enviar_dos_veces_devuelve_409()
    {
        await _factory.EnsureDatabaseMigratedAsync();

        var client = _factory.CreateClient();
        var token = _factory.CreateTokenWithPermissions(
            PermissionCodes.ComprasRegistrar,
            PermissionCodes.ProductoEditar,
            PermissionCodes.ProductoVer);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var proveedorId = await TestData.CreateProveedorAsync(client);
        var sku = $"SKU-{Guid.NewGuid():N}";
        var productResponse = await client.PostAsJsonAsync("/api/v1/productos", new ProductoCrearDto(
            $"Producto {Guid.NewGuid():N}",
            sku,
            null,
            null,
            proveedorId,
            true));

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var product = await productResponse.Content.ReadFromJsonAsync<ProductoDetalleDto>();
        Assert.NotNull(product);

        var ordenResponse = await client.PostAsJsonAsync("/api/v1/ordenes-compra", new OrdenCompraCreateDto(
            null,
            new[]
            {
                new OrdenCompraItemCreateDto(product!.Id, 2m)
            }));

        Assert.Equal(HttpStatusCode.Created, ordenResponse.StatusCode);
        var orden = await ordenResponse.Content.ReadFromJsonAsync<OrdenCompraDto>();
        Assert.NotNull(orden);

        var enviarResponse = await client.PostAsync($"/api/v1/ordenes-compra/{orden!.Id}/enviar", content: null);
        Assert.Equal(HttpStatusCode.OK, enviarResponse.StatusCode);

        var enviarAgain = await client.PostAsync($"/api/v1/ordenes-compra/{orden.Id}/enviar", content: null);
        Assert.Equal(HttpStatusCode.Conflict, enviarAgain.StatusCode);
    }
}



