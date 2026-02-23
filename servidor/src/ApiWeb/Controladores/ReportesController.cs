using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Reportes;
using Servidor.Aplicacion.CasosDeUso.Reportes;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/reportes")]
public sealed class ReportesController : ControllerBase
{
    private readonly ReportesService _servicio;

    public ReportesController(ReportesService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("resumen-ventas")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportResumenVentasDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportResumenVentasDto>> ResumenVentas(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerResumenVentasAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("ventas-por-dia")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportChartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportChartDto>> VentasPorDia(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerVentasPorDiaAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("medios-pago")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportChartDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportChartDto>> MediosPago(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerMediosPagoAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("top-productos")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportTableDto<TopProductoItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportTableDto<TopProductoItemDto>>> TopProductos(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] int? top,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerTopProductosAsync(desde, hasta, top, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("rotacion-stock")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportTableDto<RotacionStockItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportTableDto<RotacionStockItemDto>>> RotacionStock(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerRotacionStockAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("stock-inmovilizado")]
    [Authorize(Policy = "PERM_REPORTES_VER")]
    [ProducesResponseType(typeof(ReportTableDto<StockInmovilizadoItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportTableDto<StockInmovilizadoItemDto>>> StockInmovilizado(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerStockInmovilizadoAsync(desde, hasta, cancellationToken);
        return Ok(resultado);
    }
}


