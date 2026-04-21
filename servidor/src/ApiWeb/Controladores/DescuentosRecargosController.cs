using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.CasosDeUso.DescuentosRecargos;
using Servidor.Aplicacion.Dtos.DescuentosRecargos;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/descuentos-recargos")]
public sealed class DescuentosRecargosController : ControllerBase
{
    private readonly DescuentoRecargoService _servicio;

    public DescuentosRecargosController(DescuentoRecargoService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_PRODUCTO_VER_O_VENTA_CREAR")]
    [ProducesResponseType(typeof(IReadOnlyList<DescuentoRecargoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DescuentoRecargoDto>>> Buscar(
        [FromQuery] string? tipo,
        [FromQuery] string? busqueda,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.BuscarAsync(tipo, busqueda, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR_O_VENTA_CREAR")]
    [ProducesResponseType(typeof(DescuentoRecargoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<DescuentoRecargoDto>> Crear(
        [FromBody] DescuentoRecargoCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creado = await _servicio.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { }, creado);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR_O_VENTA_CREAR")]
    [ProducesResponseType(typeof(DescuentoRecargoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DescuentoRecargoDto>> Actualizar(
        Guid id,
        [FromBody] DescuentoRecargoUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var actualizado = await _servicio.ActualizarAsync(id, solicitud, cancellationToken);
        return Ok(actualizado);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR_O_VENTA_CREAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(Guid id, CancellationToken cancellationToken)
    {
        await _servicio.EliminarAsync(id, cancellationToken);
        return NoContent();
    }
}
