using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Stock;
using Servidor.Aplicacion.CasosDeUso.Stock;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/stock")]
public sealed class StockController : ControllerBase
{
    private readonly StockService _servicioStock;

    public StockController(StockService stockService)
    {
        _servicioStock = stockService;
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpPost("adjust")]
    public ActionResult<object> Ajustar([FromBody] StockAdjustRequest solicitud)
    {
        return Ok(new { status = "ok" });
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpPost("ajustes")]
    [ProducesResponseType(typeof(StockMovimientoDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<StockMovimientoDto>> RegistrarAjuste(
        [FromBody] StockMovimientoCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var movimiento = await _servicioStock.RegistrarMovimientoAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerMovimientos), new { }, movimiento);
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpGet("saldos")]
    [ProducesResponseType(typeof(IReadOnlyList<StockSaldoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockSaldoDto>>> ObtenerSaldos(
        [FromQuery] string? search,
        [FromQuery] Guid? proveedorId,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioStock.ObtenerSaldosAsync(search, proveedorId, cancellationToken);
        return Ok(resultado);
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpGet("movimientos")]
    [ProducesResponseType(typeof(IReadOnlyList<StockMovimientoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovimientoDto>>> ObtenerMovimientos(
        [FromQuery] Guid? productoId,
        [FromQuery] long? ventaNumero,
        [FromQuery] bool? facturada,
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioStock.ObtenerMovimientosAsync(productoId, ventaNumero, facturada, desde, hasta, cancellationToken);
        return Ok(resultado);
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(IReadOnlyList<StockAlertaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockAlertaDto>>> ObtenerAlertas(
        [FromQuery] Guid? proveedorId,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioStock.ObtenerAlertasAsync(proveedorId, cancellationToken);
        return Ok(resultado);
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpPost("sugerido-compra")]
    [ProducesResponseType(typeof(StockSugeridoCompraDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockSugeridoCompraDto>> ObtenerSugeridoCompra(CancellationToken cancellationToken)
    {
        var resultado = await _servicioStock.ObtenerSugeridoCompraAsync(cancellationToken);
        return Ok(resultado);
    }

    [Authorize(Policy = "PERM_STOCK_AJUSTAR")]
    [HttpPost("alertas/remito")]
    public async Task<IActionResult> GenerarRemitoAlertas(
        [FromBody] StockRemitoRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        var pdf = await _servicioStock.GenerarRemitoAlertasAsync(solicitud, cancellationToken);
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        return File(pdf, "application/pdf", "remito-alertas.pdf");
    }
}

public sealed record StockAdjustRequest(Guid ProductId, decimal Quantity);


