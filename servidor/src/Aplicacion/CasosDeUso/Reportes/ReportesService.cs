using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Reportes;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Reportes;

public sealed class ReportesService
{
    private const int TopPorDefecto = 10;
    private const int TopMaximo = 50;

    private readonly IReportesRepository _repositorio;
    private readonly IRequestContext _contextoSolicitud;

    public ReportesService(
        IReportesRepository repositorio,
        IRequestContext contextoSolicitud)
    {
        _repositorio = repositorio;
        _contextoSolicitud = contextoSolicitud;
    }

    public async Task<ReportChartDto> ObtenerVentasPorDiaAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var datos = await _repositorio.GetVentasPorDiaAsync(tenantId, sucursalId, desde, hasta, cancellationToken);

        var etiquetas = new List<string>();
        var valores = new List<decimal>();

        if (datos.Count > 0 && desde.HasValue && hasta.HasValue)
        {
            var startDate = desde.Value.Date;
            var endDate = hasta.Value.Date;
            var mapa = datos.ToDictionary(d => d.Fecha.Date, d => d.Total);

            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                etiquetas.Add(day.ToString("yyyy-MM-dd"));
                valores.Add(mapa.TryGetValue(day.Date, out var total) ? total : 0m);
            }
        }
        else
        {
            foreach (var fila in datos.OrderBy(d => d.Fecha))
            {
                etiquetas.Add(fila.Fecha.ToString("yyyy-MM-dd"));
                valores.Add(fila.Total);
            }
        }

        var series = new List<ReportSerieDto>
        {
            new("Ventas", valores)
        };

        return new ReportChartDto(etiquetas, series);
    }

    public async Task<ReportResumenVentasDto> ObtenerResumenVentasAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        return await _repositorio.GetResumenVentasAsync(tenantId, sucursalId, desde, hasta, cancellationToken);
    }

    public async Task<ReportChartDto> ObtenerMediosPagoAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var datos = await _repositorio.GetMediosPagoAsync(tenantId, sucursalId, desde, hasta, cancellationToken);
        var etiquetas = datos.Select(d => d.MedioPago).ToList();
        var valores = datos.Select(d => d.Total).ToList();

        var series = new List<ReportSerieDto>
        {
            new("Total", valores)
        };

        return new ReportChartDto(etiquetas, series);
    }

    public async Task<ReportTableDto<TopProductoItemDto>> ObtenerTopProductosAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int? top,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var topFinal = top ?? TopPorDefecto;
        if (topFinal <= 0 || topFinal > TopMaximo)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["top"] = new[] { $"Top debe estar entre 1 y {TopMaximo}." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var datos = await _repositorio.GetTopProductosAsync(tenantId, sucursalId, desde, hasta, topFinal, cancellationToken);
        return new ReportTableDto<TopProductoItemDto>(datos);
    }

    public async Task<ReportTableDto<RotacionStockItemDto>> ObtenerRotacionStockAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var datos = await _repositorio.GetRotacionStockAsync(tenantId, sucursalId, desde, hasta, cancellationToken);
        return new ReportTableDto<RotacionStockItemDto>(datos);
    }

    public async Task<ReportTableDto<StockInmovilizadoItemDto>> ObtenerStockInmovilizadoAsync(
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken)
    {
        (desde, hasta) = NormalizarRangoAUtc(desde, hasta);
        ValidarRango(desde, hasta);

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var datos = await _repositorio.GetStockInmovilizadoAsync(tenantId, sucursalId, desde, hasta, cancellationToken);
        return new ReportTableDto<StockInmovilizadoItemDto>(datos);
    }

    private static void ValidarRango(DateTimeOffset? desde, DateTimeOffset? hasta)
    {
        if (desde.HasValue && hasta.HasValue && desde > hasta)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["fecha"] = new[] { "El rango de fechas es invalido." }
                });
        }
    }

    private static (DateTimeOffset? Desde, DateTimeOffset? Hasta) NormalizarRangoAUtc(
        DateTimeOffset? desde,
        DateTimeOffset? hasta)
    {
        var desdeUtc = desde?.ToUniversalTime();
        var hastaUtc = hasta?.ToUniversalTime();
        return (desdeUtc, hastaUtc);
    }

    private Guid AsegurarTenant()
    {
        if (_contextoSolicitud.TenantId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        return _contextoSolicitud.TenantId;
    }

    private Guid AsegurarSucursal()
    {
        if (_contextoSolicitud.SucursalId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de sucursal invalido.");
        }

        return _contextoSolicitud.SucursalId;
    }
}


