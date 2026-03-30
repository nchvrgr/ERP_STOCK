using Microsoft.EntityFrameworkCore;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Reportes;
using Servidor.Dominio.Enums;
using Servidor.Infraestructura.Persistence;

namespace Servidor.Infraestructura.Repositories;

public sealed class ReportesRepository : IReportesRepository
{
    private readonly PosDbContext _dbContext;

    public ReportesRepository(PosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReportResumenVentasDto> GetResumenVentasAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var ventasRaw = await _dbContext.Ventas.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Estado == VentaEstado.Confirmada)
            .Select(v => new { v.Id, v.TotalNeto, v.Facturada, v.UpdatedAt })
            .ToListAsync(cancellationToken);

        var ventas = ApplyRange(ventasRaw, v => v.UpdatedAt, desde, hasta).ToList();

        var totalIngresos = ventas.Sum(v => v.TotalNeto);
        var totalFacturado = ventas.Where(v => v.Facturada).Sum(v => v.TotalNeto);
        var totalNoFacturado = ventas.Where(v => !v.Facturada).Sum(v => v.TotalNeto);

        var costoProductosIngresadosRows = await (
            from item in _dbContext.StockMovimientoItems.AsNoTracking()
            join mov in _dbContext.StockMovimientos.AsNoTracking() on item.MovimientoId equals mov.Id
            join producto in _dbContext.Productos.AsNoTracking() on item.ProductoId equals producto.Id
            where item.TenantId == tenantId
                  && mov.TenantId == tenantId
                  && mov.SucursalId == sucursalId
                  && producto.TenantId == tenantId
                  && item.EsIngreso
            select new
            {
                Monto = item.Cantidad * producto.PrecioBase,
                mov.Fecha
            })
            .ToListAsync(cancellationToken);

        var costoProductosIngresados = ApplyRange(costoProductosIngresadosRows, x => x.Fecha, desde, hasta)
            .Sum(x => x.Monto);

        var egresosCajaRows = await _dbContext.CajaMovimientos.AsNoTracking()
            .Where(m => m.TenantId == tenantId
                        && (m.Tipo == CajaMovimientoTipo.Retiro
                            || m.Tipo == CajaMovimientoTipo.Gasto
                            || m.Tipo == CajaMovimientoTipo.Egreso))
            .Select(x => new { x.Fecha, x.Monto })
            .ToListAsync(cancellationToken);

        var egresosCaja = ApplyRange(egresosCajaRows, x => x.Fecha, desde, hasta)
            .Sum(x => x.Monto);

        var diferenciasNegativasRows = await _dbContext.CajaSesiones.AsNoTracking()
            .Where(s => s.TenantId == tenantId
                        && s.SucursalId == sucursalId
                        && s.CierreAt != null
                        && s.DiferenciaTotal < 0)
            .Select(x => new { x.CierreAt, x.DiferenciaTotal })
            .ToListAsync(cancellationToken);

        var diferenciasNegativas = ApplyRange(
                diferenciasNegativasRows.Where(x => x.CierreAt.HasValue),
                x => x.CierreAt!.Value,
                desde,
                hasta)
            .Sum(x => -x.DiferenciaTotal);

        var totalEgresos = costoProductosIngresados + egresosCaja + diferenciasNegativas;

        return new ReportResumenVentasDto(totalIngresos, totalEgresos, totalFacturado, totalNoFacturado);
    }

    public async Task<IReadOnlyList<VentaPorDiaItemDto>> GetVentasPorDiaAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var raw = await _dbContext.Ventas.AsNoTracking()
            .Where(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Estado == VentaEstado.Confirmada)
            .Select(v => new { v.UpdatedAt, v.TotalNeto })
            .ToListAsync(cancellationToken);

        var result = ApplyRange(raw, v => v.UpdatedAt, desde, hasta)
            .GroupBy(v => v.UpdatedAt.UtcDateTime.Date)
            .Select(g => new VentaPorDiaItemDto(g.Key, g.Sum(x => x.TotalNeto)))
            .OrderBy(x => x.Fecha)
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<MedioPagoItemDto>> GetMediosPagoAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var raw = await (
            from pago in _dbContext.VentaPagos.AsNoTracking()
            join venta in _dbContext.Ventas.AsNoTracking() on pago.VentaId equals venta.Id
            where pago.TenantId == tenantId
                  && venta.TenantId == tenantId
                  && venta.SucursalId == sucursalId
                  && venta.Estado == VentaEstado.Confirmada
            select new { pago.MedioPago, pago.Monto, venta.UpdatedAt })
            .ToListAsync(cancellationToken);

        var result = ApplyRange(raw, x => x.UpdatedAt, desde, hasta)
            .GroupBy(x => x.MedioPago)
            .Select(g => new MedioPagoItemDto(g.Key, g.Sum(x => x.Monto)))
            .OrderByDescending(x => x.Total)
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<TopProductoItemDto>> GetTopProductosAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        int top,
        CancellationToken cancellationToken = default)
    {
        var raw = await (
            from item in _dbContext.VentaItems.AsNoTracking()
            join venta in _dbContext.Ventas.AsNoTracking() on item.VentaId equals venta.Id
            join producto in _dbContext.Productos.AsNoTracking() on item.ProductoId equals producto.Id
            where item.TenantId == tenantId
                  && venta.TenantId == tenantId
                  && venta.SucursalId == sucursalId
                  && venta.Estado == VentaEstado.Confirmada
                  && producto.TenantId == tenantId
            select new
            {
                item.ProductoId,
                producto.Name,
                producto.Sku,
                item.Cantidad,
                item.PrecioUnitario,
                venta.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var result = ApplyRange(raw, x => x.UpdatedAt, desde, hasta)
            .GroupBy(x => new { x.ProductoId, x.Name, x.Sku })
            .Select(g => new TopProductoItemDto(
                g.Key.ProductoId,
                g.Key.Name,
                g.Key.Sku,
                g.Sum(x => x.Cantidad),
                g.Sum(x => x.Cantidad * x.PrecioUnitario)))
            .OrderByDescending(x => x.Total)
            .Take(top)
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<RotacionStockItemDto>> GetRotacionStockAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var raw = await (
            from item in _dbContext.StockMovimientoItems.AsNoTracking()
            join mov in _dbContext.StockMovimientos.AsNoTracking() on item.MovimientoId equals mov.Id
            join producto in _dbContext.Productos.AsNoTracking() on item.ProductoId equals producto.Id
            where item.TenantId == tenantId
                  && mov.TenantId == tenantId
                  && mov.SucursalId == sucursalId
                  && producto.TenantId == tenantId
            select new
            {
                item.ProductoId,
                producto.Name,
                producto.Sku,
                item.Cantidad,
                item.EsIngreso,
                mov.Fecha
            })
            .ToListAsync(cancellationToken);

        var result = ApplyRange(raw, x => x.Fecha, desde, hasta)
            .GroupBy(x => new { x.ProductoId, x.Name, x.Sku })
            .Select(g => new RotacionStockItemDto(
                g.Key.ProductoId,
                g.Key.Name,
                g.Key.Sku,
                g.Where(x => x.EsIngreso).Sum(x => x.Cantidad),
                g.Where(x => !x.EsIngreso).Sum(x => x.Cantidad),
                g.Where(x => x.EsIngreso).Sum(x => x.Cantidad) - g.Where(x => !x.EsIngreso).Sum(x => x.Cantidad)))
            .OrderByDescending(x => x.Salidas)
            .ToList();

        return result;
    }

    public async Task<IReadOnlyList<StockInmovilizadoItemDto>> GetStockInmovilizadoAsync(
        Guid tenantId,
        Guid sucursalId,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default)
    {
        var ventasItemsQuery =
            from item in _dbContext.VentaItems.AsNoTracking()
            join venta in _dbContext.Ventas.AsNoTracking() on item.VentaId equals venta.Id
            where item.TenantId == tenantId
                  && venta.TenantId == tenantId
                  && venta.SucursalId == sucursalId
                  && venta.Estado == VentaEstado.Confirmada
            select new
            {
                item.ProductoId,
                item.Cantidad,
                venta.UpdatedAt
            };

        var ventasItems = ApplyRange(
                await ventasItemsQuery.ToListAsync(cancellationToken),
                x => x.UpdatedAt,
                desde,
                hasta)
            .ToList();

        var vendidosByProducto = ventasItems
            .GroupBy(x => x.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                CantidadVendida = g.Sum(x => x.Cantidad)
            })
            .ToDictionary(x => x.ProductoId, x => x.CantidadVendida);

        var saldosQuery = _dbContext.StockSaldos.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.SucursalId == sucursalId);

        var saldos = await saldosQuery.ToListAsync(cancellationToken);

        var saldosByProducto = saldos
            .GroupBy(s => s.ProductoId)
            .Select(g => new
            {
                ProductoId = g.Key,
                Stock = g.Sum(x => x.CantidadActual)
            })
            .ToDictionary(x => x.ProductoId, x => x.Stock);

        var movimientos = await (
            from item in _dbContext.StockMovimientoItems.AsNoTracking()
            join mov in _dbContext.StockMovimientos.AsNoTracking() on item.MovimientoId equals mov.Id
            where item.TenantId == tenantId
                  && mov.TenantId == tenantId
                  && mov.SucursalId == sucursalId
            select new
            {
                item.ProductoId,
                mov.Fecha
            })
            .ToListAsync(cancellationToken);

        var lastMovimientos = movimientos
            .GroupBy(x => x.ProductoId)
            .ToDictionary(
                g => g.Key,
                g => (DateTimeOffset?)g.Max(x => x.Fecha));

        var productos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Sku,
                p.CreatedAt
            })
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var nowUtc = DateTimeOffset.UtcNow;
        var result = productos
            .Where(p => !vendidosByProducto.TryGetValue(p.Id, out var qty) || qty <= 0)
            .Select(p =>
            {
                var ultimo = lastMovimientos.TryGetValue(p.Id, out var mov) ? mov : null;
                var stock = saldosByProducto.TryGetValue(p.Id, out var st) ? st : 0m;
                var baseDate = ultimo ?? p.CreatedAt;
                var diasSinMovimiento = Math.Max(0, (int)Math.Floor((nowUtc - baseDate).TotalDays));
                return new StockInmovilizadoItemDto(
                    p.Id,
                    p.Name,
                    p.Sku,
                    stock,
                    ultimo,
                    diasSinMovimiento);
            })
            .ToList();

        return result;
    }

    private static IEnumerable<T> ApplyRange<T>(
        IEnumerable<T> source,
        Func<T, DateTimeOffset> selector,
        DateTimeOffset? desde,
        DateTimeOffset? hasta)
    {
        if (desde.HasValue)
        {
            source = source.Where(x => selector(x) >= desde.Value);
        }

        if (hasta.HasValue)
        {
            source = source.Where(x => selector(x) <= hasta.Value);
        }

        return source;
    }
}


