using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Ventas;
using Servidor.Aplicacion.CasosDeUso.Ventas;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/ventas")]
public sealed class VentasController : ControllerBase
{
    private readonly VentaService _servicioVenta;

    public VentasController(VentaService servicioVenta)
    {
        _servicioVenta = servicioVenta;
    }

    [HttpPost]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<VentaDto>> IniciarVenta(CancellationToken cancellationToken)
    {
        var venta = await _servicioVenta.IniciarVentaAsync(cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = venta.Id }, venta);
    }

    [HttpPost("{id:guid}/items/scan")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaItemDto>> EscanearItem(
        Guid id,
        [FromBody] VentaScanRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var item = await _servicioVenta.AgregarItemPorCodigoAsync(id, solicitud, cancellationToken);
        return Ok(item);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaItemDto>> AgregarItem(
        Guid id,
        [FromBody] VentaItemByProductRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var item = await _servicioVenta.AgregarItemPorProductoAsync(id, solicitud, cancellationToken);
        return Ok(item);
    }

    [HttpPatch("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaItemDto>> ActualizarItem(
        Guid id,
        Guid itemId,
        [FromBody] VentaItemUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var item = await _servicioVenta.ActualizarItemAsync(id, itemId, solicitud, cancellationToken);
        return Ok(item);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaItemDto>> QuitarItem(
        Guid id,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var item = await _servicioVenta.QuitarItemAsync(id, itemId, cancellationToken);
        return Ok(item);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaDto>> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        var venta = await _servicioVenta.ObtenerPorIdAsync(id, cancellationToken);
        return Ok(venta);
    }

    [HttpGet("numero/{numero:long}/ticket")]
    [Authorize(Policy = "PERM_VENTA_CREAR")]
    [ProducesResponseType(typeof(VentaTicketDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaTicketDto>> ObtenerTicketPorNumero(
        long numero,
        CancellationToken cancellationToken)
    {
        var ticket = await _servicioVenta.ObtenerTicketPorNumeroAsync(numero, cancellationToken);
        return Ok(ticket);
    }

    [HttpPost("{id:guid}/confirmar")]
    [Authorize(Policy = "PERM_VENTA_CONFIRMAR")]
    [ProducesResponseType(typeof(VentaConfirmResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaConfirmResultDto>> Confirmar(
        Guid id,
        [FromBody] VentaConfirmRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioVenta.ConfirmarAsync(id, solicitud, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost("{id:guid}/anular")]
    [Authorize(Policy = "PERM_VENTA_ANULAR")]
    [ProducesResponseType(typeof(VentaAnularResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<VentaAnularResultDto>> Anular(
        Guid id,
        [FromBody] VentaAnularRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioVenta.AnularAsync(id, solicitud, cancellationToken);
        return Ok(resultado);
    }
}


