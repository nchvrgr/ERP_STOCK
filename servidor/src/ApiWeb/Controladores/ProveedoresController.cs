using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Proveedores;
using Servidor.Aplicacion.CasosDeUso.Proveedores;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/proveedores")]
public sealed class ProveedoresController : ControllerBase
{
    private readonly ProveedorService _servicioProveedor;

    public ProveedoresController(ProveedorService servicioProveedor)
    {
        _servicioProveedor = servicioProveedor;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(typeof(IReadOnlyList<ProveedorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProveedorDto>>> Buscar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var resultados = await _servicioProveedor.BuscarAsync(busqueda, activo, cancellationToken);
        return Ok(resultados);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProveedorDto>> Crear(
        [FromBody] ProveedorCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creado = await _servicioProveedor.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { }, creado);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(typeof(ProveedorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProveedorDto>> Actualizar(
        Guid id,
        [FromBody] ProveedorUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var actualizado = await _servicioProveedor.ActualizarAsync(id, solicitud, cancellationToken);
        return Ok(actualizado);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await _servicioProveedor.EliminarAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/delete-preview")]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(typeof(ProveedorDeletePreviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProveedorDeletePreviewDto>> ObtenerVistaPreviaEliminacion(
        Guid id,
        CancellationToken cancellationToken)
    {
        var vistaPrevia = await _servicioProveedor.ObtenerVistaPreviaEliminacionAsync(id, cancellationToken);
        return Ok(vistaPrevia);
    }

    [HttpPost("{id:guid}/delete")]
    [Authorize(Policy = "PERM_PROVEEDOR_GESTIONAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarConResolucionDeProductos(
        Guid id,
        [FromBody] ProveedorDeleteRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        await _servicioProveedor.EliminarConResolucionDeProductosAsync(id, solicitud, cancellationToken);
        return NoContent();
    }
}


