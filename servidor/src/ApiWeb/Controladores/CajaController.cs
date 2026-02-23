using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Aplicacion.CasosDeUso.Caja;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/caja")]
public sealed class CajaController : ControllerBase
{
    private readonly CajaService _servicioCaja;

    public CajaController(CajaService servicioCaja)
    {
        _servicioCaja = servicioCaja;
    }

    [HttpGet]
    [Authorize(Policy = "ROLE_ENCARGADO_ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<CajaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CajaDto>>> ObtenerCajas(
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var cajas = await _servicioCaja.ObtenerCajasAsync(activo, cancellationToken);
        return Ok(cajas);
    }

    [HttpPost]
    [Authorize(Policy = "ROLE_ENCARGADO_ADMIN")]
    [ProducesResponseType(typeof(CajaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CajaDto>> CrearCaja(
        [FromBody] CajaCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creada = await _servicioCaja.CrearCajaAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerCajas), new { }, creada);
    }

    [HttpPost("sesiones/abrir")]
    [Authorize(Policy = "ROLE_ENCARGADO_ADMIN")]
    [ProducesResponseType(typeof(CajaSesionDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CajaSesionDto>> AbrirSesion(
        [FromBody] CajaSesionAbrirDto solicitud,
        CancellationToken cancellationToken)
    {
        var sesion = await _servicioCaja.AbrirSesionAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerResumen), new { id = sesion.Id }, sesion);
    }

    [HttpPost("sesiones/{id:guid}/movimientos")]
    [Authorize(Policy = "PERM_CAJA_MOVIMIENTO")]
    [ProducesResponseType(typeof(CajaMovimientoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CajaMovimientoDto>> RegistrarMovimiento(
        Guid id,
        [FromBody] CajaMovimientoCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var movimiento = await _servicioCaja.RegistrarMovimientoAsync(id, solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerResumen), new { id }, movimiento);
    }

    [HttpPost("sesiones/{id:guid}/cerrar")]
    [Authorize(Policy = "ROLE_ENCARGADO_ADMIN")]
    [ProducesResponseType(typeof(CajaCierreResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CajaCierreResultDto>> CerrarSesion(
        Guid id,
        [FromBody] CajaCierreRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioCaja.CerrarSesionAsync(id, solicitud, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("sesiones/{id:guid}/resumen")]
    [Authorize(Policy = "PERM_CAJA_MOVIMIENTO")]
    [ProducesResponseType(typeof(CajaResumenDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CajaResumenDto>> ObtenerResumen(Guid id, CancellationToken cancellationToken)
    {
        var resumen = await _servicioCaja.ObtenerResumenAsync(id, cancellationToken);
        return Ok(resumen);
    }

    [HttpGet("sesiones/historial")]
    [Authorize(Policy = "ROLE_ENCARGADO_ADMIN")]
    [ProducesResponseType(typeof(IReadOnlyList<CajaHistorialDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CajaHistorialDto>>> ObtenerHistorial(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioCaja.ObtenerHistorialAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }
}


