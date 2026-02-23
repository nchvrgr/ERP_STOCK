using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Aplicacion.Dtos.Stock;
using Servidor.Aplicacion.CasosDeUso.Productos;
using Servidor.Aplicacion.CasosDeUso.Stock;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/productos")]
public sealed class ProductosController : ControllerBase
{
    private readonly ServicioProductos _servicioProductos;
    private readonly StockService _servicioStock;

    public ProductosController(ServicioProductos servicioProductos, StockService stockService)
    {
        _servicioProductos = servicioProductos;
        _servicioStock = stockService;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_PRODUCTO_VER")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoListaItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductoListaItemDto>>> Buscar(
        [FromQuery] string? search,
        [FromQuery] Guid? categoriaId,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var resultados = await _servicioProductos.BuscarAsync(search, categoriaId, activo, cancellationToken);
        return Ok(resultados);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ProductoDetalleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductoDetalleDto>> Crear(
        [FromBody] ProductoCrearDto solicitud,
        CancellationToken cancellationToken)
    {
        var creado = await _servicioProductos.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_VER")]
    [ProducesResponseType(typeof(ProductoDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoDetalleDto>> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        var producto = await _servicioProductos.ObtenerPorIdAsync(id, cancellationToken);
        return Ok(producto);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ProductoDetalleDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoDetalleDto>> Actualizar(
        Guid id,
        [FromBody] ProductoActualizarDto solicitud,
        CancellationToken cancellationToken)
    {
        var actualizado = await _servicioProductos.ActualizarAsync(id, solicitud, cancellationToken);
        return Ok(actualizado);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await _servicioProductos.EliminarAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/codigos")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ProductoCodigoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductoCodigoDto>> AgregarCodigo(
        Guid id,
        [FromBody] ProductoCodigoCrearDto solicitud,
        CancellationToken cancellationToken)
    {
        var codigo = await _servicioProductos.AgregarCodigoAsync(id, solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, codigo);
    }

    [HttpDelete("{id:guid}/codigos/{codigoId:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> QuitarCodigo(Guid id, Guid codigoId, CancellationToken cancellationToken)
    {
        await _servicioProductos.QuitarCodigoAsync(id, codigoId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/proveedores")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ProductoProveedorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductoProveedorDto>> AgregarProveedor(
        Guid id,
        [FromBody] ProductoProveedorCrearDto solicitud,
        CancellationToken cancellationToken)
    {
        var relacion = await _servicioProductos.AgregarProveedorAsync(id, solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id }, relacion);
    }

    [HttpPatch("{id:guid}/proveedores/{relId:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ProductoProveedorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductoProveedorDto>> DefinirProveedorPrincipal(
        Guid id,
        Guid relId,
        [FromBody] ProductoProveedorActualizarDto solicitud,
        CancellationToken cancellationToken)
    {
        var relacion = await _servicioProductos.DefinirProveedorPrincipalAsync(id, relId, solicitud, cancellationToken);
        return Ok(relacion);
    }

    [HttpPatch("{id:guid}/stock-config")]
    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [ProducesResponseType(typeof(StockConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockConfigDto>> ActualizarConfiguracionStock(
        Guid id,
        [FromBody] StockConfigUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var configuracion = await _servicioStock.ActualizarConfiguracionStockAsync(id, solicitud, cancellationToken);
        return Ok(configuracion);
    }

    [HttpGet("{id:guid}/stock-config")]
    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [ProducesResponseType(typeof(StockConfigDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockConfigDto>> ObtenerConfiguracionStock(Guid id, CancellationToken cancellationToken)
    {
        var configuracion = await _servicioStock.ObtenerConfiguracionStockAsync(id, cancellationToken);
        return Ok(configuracion);
    }
}







