using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Servidor.Aplicacion.Dtos.Etiquetas;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Dominio.Enums;
using Xunit;

namespace Servidor.Pruebas;

public sealed class EtiquetasTests : IClassFixture<WebApiFactory>
{
    private readonly WebApiFactory _factory;

    public EtiquetasTests(WebApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Etiquetas_endpoint_devuelve_pdf()
    {
        await _factory.EnsureDatabaseMigratedAsync();

        var client = _factory.CreateClient();
        var token = _factory.CreateTokenWithPermissions(
            PermissionCodes.ProductoEditar,
            PermissionCodes.ProductoVer);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

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

        var request = new EtiquetaRequestDto(new[] { product!.Id });
        var response = await client.PostAsJsonAsync("/api/v1/etiquetas/pdf", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
    }
}



