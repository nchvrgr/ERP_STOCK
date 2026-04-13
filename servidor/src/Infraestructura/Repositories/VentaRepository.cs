using Microsoft.EntityFrameworkCore;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Ventas;
using Servidor.Aplicacion.Dtos.Precios;
using Servidor.Aplicacion.Dtos.Stock;
using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Dominio.Entities;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;
using Servidor.Infraestructura.Persistence;
using System.Text.Json;

namespace Servidor.Infraestructura.Repositories;

public sealed class VentaRepository : IVentaRepository
{
    private readonly PosDbContext _dbContext;
    private sealed record ProductStockDefinition(Guid ProductoId, bool IsCombo, string? ComboItemsJson);
    private sealed record StockDemandItem(Guid ProductoId, decimal Cantidad);

    private static readonly JsonSerializerOptions ComboJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public VentaRepository(PosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static decimal RoundMoney(decimal amount) =>
        Math.Round(amount, 2, MidpointRounding.AwayFromZero);

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid userId,
        string listaPrecio,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var numero = await GetNextVentaNumeroAsync(cancellationToken);
        var venta = new Venta(Guid.NewGuid(), tenantId, sucursalId, userId, listaPrecio, nowUtc, numero);
        _dbContext.Ventas.Add(venta);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return venta.Id;
    }

    private async Task<long> GetNextVentaNumeroAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsNpgsql())
        {
            var next = await _dbContext.Database
                .SqlQuery<long>($"SELECT nextval('venta_numero_seq') AS \"Value\"")
                .SingleAsync(cancellationToken);
            return next;
        }

        var currentMax = await _dbContext.Ventas
            .MaxAsync(v => (long?)v.Numero, cancellationToken) ?? 0L;

        return currentMax + 1L;
    }

    public async Task<VentaDto?> GetByIdAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _dbContext.Ventas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            return null;
        }

        var items = await (from i in _dbContext.VentaItems.AsNoTracking()
                join p in _dbContext.Productos.AsNoTracking() on i.ProductoId equals p.Id
                where i.TenantId == tenantId && i.VentaId == ventaId && p.TenantId == tenantId
                orderby p.Name
                select new VentaItemDto(
                    i.Id,
                    i.ProductoId,
                    p.Name,
                    p.Sku,
                    i.Codigo,
                    i.Cantidad,
                    i.PrecioUnitario,
                    i.Cantidad * i.PrecioUnitario))
            .ToListAsync(cancellationToken);

        return new VentaDto(
            venta.Id,
            venta.Numero,
            venta.SucursalId,
            venta.UserId,
            venta.Estado.ToString().ToUpperInvariant(),
            venta.ListaPrecio,
            venta.TotalNeto,
            venta.TotalPagos,
            venta.CreatedAt,
            items,
            venta.Facturada);
    }

    public async Task<VentaTicketDto?> GetTicketByNumeroAsync(
        Guid tenantId,
        Guid sucursalId,
        long numeroVenta,
        CancellationToken cancellationToken = default)
    {
        var venta = await _dbContext.Ventas.AsNoTracking()
            .FirstOrDefaultAsync(
                v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Numero == numeroVenta,
                cancellationToken);

        if (venta is null)
        {
            return null;
        }

        var items = await (from i in _dbContext.VentaItems.AsNoTracking()
                join p in _dbContext.Productos.AsNoTracking() on i.ProductoId equals p.Id
                where i.TenantId == tenantId && i.VentaId == venta.Id && p.TenantId == tenantId
                orderby p.Name
                select new VentaItemDto(
                    i.Id,
                    i.ProductoId,
                    p.Name,
                    p.Sku,
                    i.Codigo,
                    i.Cantidad,
                    i.PrecioUnitario,
                    i.Cantidad * i.PrecioUnitario))
            .ToListAsync(cancellationToken);

        var pagos = (await _dbContext.VentaPagos.AsNoTracking()
                .Where(vp => vp.TenantId == tenantId && vp.VentaId == venta.Id)
                .Select(vp => new
                {
                    vp.Id,
                    vp.MedioPago,
                    vp.Monto,
                    vp.CreatedAt
                })
                .ToListAsync(cancellationToken))
            .OrderBy(vp => vp.CreatedAt)
            .Select(vp => new VentaPagoDto(vp.Id, vp.MedioPago, vp.Monto))
            .ToList();

        var ventaDto = new VentaDto(
            venta.Id,
            venta.Numero,
            venta.SucursalId,
            venta.UserId,
            venta.Estado.ToString().ToUpperInvariant(),
            venta.ListaPrecio,
            venta.TotalNeto,
            venta.TotalPagos,
            venta.CreatedAt,
            items,
            venta.Facturada);

        return new VentaTicketDto(ventaDto, pagos);
    }

    public async Task<VentaItemChangeDto> AddItemByCodeAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        string code,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Borrador)
        {
            throw new ConflictException("La venta no esta en borrador.");
        }

        var cajaAbierta = await _dbContext.CajaSesiones.AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.SucursalId == sucursalId && s.Estado == CajaSesionEstado.Abierta, cancellationToken);
        if (!cajaAbierta)
        {
            throw new ConflictException("No hay caja abierta.");
        }

        var normalizedCode = code.Trim();
        var product = await (from p in _dbContext.Productos.AsNoTracking()
                join pc in _dbContext.ProductoCodigos.AsNoTracking().Where(x => x.TenantId == tenantId)
                    on p.Id equals pc.ProductoId into codigos
                from pc in codigos.DefaultIfEmpty()
                where p.TenantId == tenantId
                      && p.IsActive
                      && (p.Sku == normalizedCode || (pc != null && pc.Codigo == normalizedCode))
                select new
                {
                    p.Id,
                    p.Name,
                    p.Sku,
                    p.PrecioBase,
                    p.PrecioVenta,
                    p.IsCombo,
                    p.ComboItemsJson,
                    Codigo = pc != null && pc.Codigo == normalizedCode ? pc.Codigo : p.Sku,
                    Prioridad = p.Sku == normalizedCode ? 0 : 1
                })
            .OrderBy(x => x.Prioridad)
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw new NotFoundException($"Producto no encontrado para SKU {normalizedCode}");
        }

        var item = await _dbContext.VentaItems
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.VentaId == ventaId && i.ProductoId == product.Id, cancellationToken);

        var cantidadSolicitada = (item?.Cantidad ?? 0m) + 1m;
        await EnsureStockDisponibleParaAgregarAsync(
            tenantId,
            sucursalId,
            product.Id,
            product.Name,
            product.IsCombo,
            product.ComboItemsJson,
            cantidadSolicitada,
            cancellationToken);

        var creado = false;
        var cantidadAntes = 0m;
        var precioUnitario = 0m;
        if (item is null)
        {
            precioUnitario = await GetPrecioUnitarioAsync(
                tenantId,
                venta.ListaPrecio,
                product.Id,
                product.PrecioVenta,
                product.PrecioBase,
                cancellationToken);
            item = new VentaItem(Guid.NewGuid(), tenantId, ventaId, product.Id, product.Codigo, 1m, precioUnitario, nowUtc);
            _dbContext.VentaItems.Add(item);
            creado = true;
        }
        else
        {
            cantidadAntes = item.Cantidad;
            item.Incrementar(1m, nowUtc);
            precioUnitario = item.PrecioUnitario;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = new VentaItemDto(
            item.Id,
            product.Id,
            product.Name,
            product.Sku,
            item.Codigo,
            item.Cantidad,
            precioUnitario,
            item.Cantidad * precioUnitario);

        return new VentaItemChangeDto(dto, cantidadAntes, item.Cantidad, creado);
    }

    public async Task<VentaItemChangeDto> AddItemByProductAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        Guid productId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Borrador)
        {
            throw new ConflictException("La venta no esta en borrador.");
        }

        var cajaAbierta = await _dbContext.CajaSesiones.AsNoTracking()
            .AnyAsync(s => s.TenantId == tenantId && s.SucursalId == sucursalId && s.Estado == CajaSesionEstado.Abierta, cancellationToken);
        if (!cajaAbierta)
        {
            throw new ConflictException("No hay caja abierta.");
        }

        var product = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Id == productId && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Sku, p.PrecioBase, p.PrecioVenta, p.IsCombo, p.ComboItemsJson })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            throw new ConflictException("Producto inactivo o no encontrado.");
        }

        var codigoFinal = product.Sku;

        var item = await _dbContext.VentaItems
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.VentaId == ventaId && i.ProductoId == product.Id, cancellationToken);

        var cantidadSolicitada = (item?.Cantidad ?? 0m) + 1m;
        await EnsureStockDisponibleParaAgregarAsync(
            tenantId,
            sucursalId,
            product.Id,
            product.Name,
            product.IsCombo,
            product.ComboItemsJson,
            cantidadSolicitada,
            cancellationToken);

        var creado = false;
        var cantidadAntes = 0m;
        var precioUnitario = 0m;
        if (item is null)
        {
            precioUnitario = await GetPrecioUnitarioAsync(
                tenantId,
                venta.ListaPrecio,
                product.Id,
                product.PrecioVenta,
                product.PrecioBase,
                cancellationToken);
            item = new VentaItem(Guid.NewGuid(), tenantId, ventaId, product.Id, codigoFinal, 1m, precioUnitario, nowUtc);
            _dbContext.VentaItems.Add(item);
            creado = true;
        }
        else
        {
            cantidadAntes = item.Cantidad;
            item.Incrementar(1m, nowUtc);
            precioUnitario = item.PrecioUnitario;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var dto = new VentaItemDto(
            item.Id,
            product.Id,
            product.Name,
            product.Sku,
            item.Codigo,
            item.Cantidad,
            precioUnitario,
            item.Cantidad * precioUnitario);

        return new VentaItemChangeDto(dto, cantidadAntes, item.Cantidad, creado);
    }

    public async Task<VentaItemDto> RemoveItemAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        Guid itemId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Borrador)
        {
            throw new ConflictException("La venta no esta en borrador.");
        }

        var item = await (from i in _dbContext.VentaItems
                join p in _dbContext.Productos.AsNoTracking() on i.ProductoId equals p.Id
                where i.TenantId == tenantId && i.VentaId == ventaId && i.Id == itemId && p.TenantId == tenantId
                select new { Item = i, p.Name, p.Sku })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("Item no encontrado.");
        }

        _dbContext.VentaItems.Remove(item.Item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new VentaItemDto(
            item.Item.Id,
            item.Item.ProductoId,
            item.Name,
            item.Sku,
            item.Item.Codigo,
            item.Item.Cantidad,
            item.Item.PrecioUnitario,
            item.Item.Cantidad * item.Item.PrecioUnitario);
    }

    public async Task<VentaItemChangeDto> UpdateItemCantidadAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        Guid itemId,
        decimal cantidad,
        decimal? precioUnitario,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Borrador)
        {
            throw new ConflictException("La venta no esta en borrador.");
        }

        var item = await _dbContext.VentaItems
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.VentaId == ventaId && i.Id == itemId, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException("Item no encontrado.");
        }

        var cantidadAntes = item.Cantidad;
        item.ActualizarCantidad(cantidad, nowUtc);
        if (precioUnitario.HasValue)
        {
            item.ActualizarPrecioUnitario(precioUnitario.Value, nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var product = await _dbContext.Productos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == item.ProductoId, cancellationToken);

        var nombre = product?.Name ?? string.Empty;
        var sku = product?.Sku ?? string.Empty;

        var dto = new VentaItemDto(
            item.Id,
            item.ProductoId,
            nombre,
            sku,
            item.Codigo,
            item.Cantidad,
            item.PrecioUnitario,
            item.Cantidad * item.PrecioUnitario);
        return new VentaItemChangeDto(dto, cantidadAntes, item.Cantidad, false);
    }

    public async Task<VentaConfirmResultDto> ConfirmAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        VentaConfirmRequestDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Borrador)
        {
            throw new ConflictException("La venta no esta en borrador.");
        }

        CajaSesion? cajaSesion;
        if (request.CajaSesionId.HasValue)
        {
            var cajaSesionId = request.CajaSesionId.Value;
            cajaSesion = await _dbContext.CajaSesiones
                .FirstOrDefaultAsync(
                    s => s.TenantId == tenantId
                         && s.SucursalId == sucursalId
                         && s.Id == cajaSesionId
                         && s.Estado == CajaSesionEstado.Abierta,
                    cancellationToken);

            if (cajaSesion is null)
            {
                throw new ConflictException("La caja indicada no esta abierta.");
            }
        }
        else
        {
            cajaSesion = await _dbContext.CajaSesiones
                .Where(s => s.TenantId == tenantId && s.SucursalId == sucursalId && s.Estado == CajaSesionEstado.Abierta)
                .OrderByDescending(s => s.AperturaAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cajaSesion is null)
            {
                throw new ConflictException("No hay caja abierta.");
            }
        }

        var items = await _dbContext.VentaItems
            .Where(i => i.TenantId == tenantId && i.VentaId == ventaId)
            .ToListAsync(cancellationToken);

        var totalNeto = RoundMoney(items.Sum(i => i.Cantidad * i.PrecioUnitario));

        var totalPagos = RoundMoney(request.Pagos?.Sum(p => p.Monto) ?? 0m);
        if (totalNeto != totalPagos)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["pagos"] = new[] { "La suma de pagos debe ser igual al total." }
                });
        }

        if (totalNeto > 0 && (request.Pagos is null || request.Pagos.Count == 0))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["pagos"] = new[] { "Debe especificar pagos." }
                });
        }

        var productInfos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.TenantId == tenantId && items.Select(i => i.ProductoId).Contains(p.Id))
            .Select(p => new { p.Id, p.IsCombo, p.ComboItemsJson })
            .ToListAsync(cancellationToken);

        var stockDefinitions = productInfos
            .Select(p => new ProductStockDefinition(p.Id, p.IsCombo, p.ComboItemsJson))
            .ToList();
        var stockDemands = items
            .Select(i => new StockDemandItem(i.ProductoId, i.Cantidad))
            .ToList();

        var requirements = BuildStockRequirements(stockDefinitions, stockDemands);
        var productIds = requirements.Keys.ToList();

        var saldos = await _dbContext.StockSaldos
            .Where(s => s.TenantId == tenantId && s.SucursalId == sucursalId && productIds.Contains(s.ProductoId))
            .ToListAsync(cancellationToken);

        var saldoByProduct = saldos.ToDictionary(s => s.ProductoId, s => s);
        var stockCambios = new List<StockSaldoChangeDto>();

        foreach (var requirement in requirements)
        {
            if (!saldoByProduct.TryGetValue(requirement.Key, out var saldo))
            {
                throw new ConflictException("Stock insuficiente.");
            }

            var before = saldo.CantidadActual;
            var after = before - requirement.Value;
            if (after < 0)
            {
                throw new ConflictException("Stock insuficiente.");
            }

            saldo.SetCantidad(after, nowUtc);
        }

        var movimientoId = Guid.NewGuid();
        var movimiento = new StockMovimiento(
            movimientoId,
            tenantId,
            sucursalId,
            StockMovimientoTipo.SalidaVenta,
            $"Venta {venta.Numero}",
            nowUtc,
            nowUtc,
            venta.Numero);

        _dbContext.StockMovimientos.Add(movimiento);

        var movimientoItems = new List<StockMovimientoItem>();
        foreach (var requirement in requirements)
        {
            var saldo = saldoByProduct[requirement.Key];
            var before = saldo.CantidadActual + requirement.Value;
            var after = saldo.CantidadActual;

            var itemId = Guid.NewGuid();
            movimientoItems.Add(new StockMovimientoItem(
                itemId,
                tenantId,
                movimientoId,
                requirement.Key,
                requirement.Value,
                false,
                nowUtc));

            stockCambios.Add(new StockSaldoChangeDto(
                movimientoId,
                itemId,
                requirement.Key,
                before,
                after));
        }

        _dbContext.StockMovimientoItems.AddRange(movimientoItems);

        venta.Confirmar(totalNeto, totalPagos, request.Facturada!.Value, nowUtc);

        var pagos = new List<VentaPago>();
        var pagosDto = new List<VentaPagoDto>();
        foreach (var pago in request.Pagos ?? Array.Empty<VentaPagoRequestDto>())
        {
            var pagoEntity = new VentaPago(
                Guid.NewGuid(),
                tenantId,
                ventaId,
                pago.MedioPago,
                pago.Monto,
                nowUtc);
            pagos.Add(pagoEntity);
            pagosDto.Add(new VentaPagoDto(pagoEntity.Id, pagoEntity.MedioPago, pagoEntity.Monto));
        }

        _dbContext.VentaPagos.AddRange(pagos);

        var cajaMovimientos = new List<CajaMovimientoResultDto>();

        var movimientosSum = (await _dbContext.CajaMovimientos
            .Where(m => m.TenantId == tenantId && m.CajaSesionId == cajaSesion.Id)
            .Select(m => m.Monto)
            .ToListAsync(cancellationToken))
            .Sum();

        var saldoActual = cajaSesion.MontoInicial + movimientosSum;

        foreach (var pago in request.Pagos ?? Array.Empty<VentaPagoRequestDto>())
        {
            var saldoAntes = saldoActual;
            saldoActual += pago.Monto;

            var movimientoCaja = new CajaMovimiento(
                Guid.NewGuid(),
                tenantId,
                cajaSesion.Id,
                CajaMovimientoTipo.Ingreso,
                pago.MedioPago,
                pago.Monto,
                $"Venta {ventaId}",
                nowUtc,
                nowUtc);

            _dbContext.CajaMovimientos.Add(movimientoCaja);

            var dto = new CajaMovimientoDto(
                movimientoCaja.Id,
                movimientoCaja.CajaSesionId,
                movimientoCaja.Tipo.ToString().ToUpperInvariant(),
                movimientoCaja.MedioPago,
                movimientoCaja.Monto,
                movimientoCaja.Motivo,
                movimientoCaja.Fecha);

            cajaMovimientos.Add(new CajaMovimientoResultDto(dto, saldoAntes, saldoActual));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var ventaDto = await GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken)
            ?? throw new NotFoundException("Venta no encontrada.");

        return new VentaConfirmResultDto(
            ventaDto,
            pagosDto,
            stockCambios,
            cajaMovimientos);
    }

    public async Task<VentaAnularResultDto> AnularAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        VentaAnularRequestDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var venta = await _dbContext.Ventas
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        if (venta.Estado != VentaEstado.Confirmada)
        {
            throw new ConflictException("La venta no esta confirmada.");
        }

        var cajaSesion = await _dbContext.CajaSesiones
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.SucursalId == sucursalId && s.Estado == CajaSesionEstado.Abierta, cancellationToken);

        if (cajaSesion is null)
        {
            throw new ConflictException("No hay caja abierta.");
        }

        var items = await _dbContext.VentaItems
            .Where(i => i.TenantId == tenantId && i.VentaId == ventaId)
            .ToListAsync(cancellationToken);

        var pagos = await _dbContext.VentaPagos.AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.VentaId == ventaId)
            .ToListAsync(cancellationToken);

        var productInfos = await _dbContext.Productos.AsNoTracking()
            .Where(p => p.TenantId == tenantId && items.Select(i => i.ProductoId).Contains(p.Id))
            .Select(p => new { p.Id, p.IsCombo, p.ComboItemsJson })
            .ToListAsync(cancellationToken);

        var stockDefinitions = productInfos
            .Select(p => new ProductStockDefinition(p.Id, p.IsCombo, p.ComboItemsJson))
            .ToList();
        var stockDemands = items
            .Select(i => new StockDemandItem(i.ProductoId, i.Cantidad))
            .ToList();

        var requirements = BuildStockRequirements(stockDefinitions, stockDemands);
        var productIds = requirements.Keys.ToList();
        var saldos = await _dbContext.StockSaldos
            .Where(s => s.TenantId == tenantId && s.SucursalId == sucursalId && productIds.Contains(s.ProductoId))
            .ToListAsync(cancellationToken);

        var saldoByProduct = saldos.ToDictionary(s => s.ProductoId, s => s);
        var addedSaldo = false;
        foreach (var productId in productIds)
        {
            if (saldoByProduct.ContainsKey(productId))
            {
                continue;
            }

            var saldo = new StockSaldo(Guid.NewGuid(), tenantId, productId, sucursalId, 0m, nowUtc);
            _dbContext.StockSaldos.Add(saldo);
            saldoByProduct[productId] = saldo;
            addedSaldo = true;
        }

        if (addedSaldo)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var movimientoId = Guid.NewGuid();
        var movimiento = new StockMovimiento(
            movimientoId,
            tenantId,
            sucursalId,
            StockMovimientoTipo.EntradaAnulacion,
            $"Anulacion venta {venta.Numero} - {request.Motivo}",
            nowUtc,
            nowUtc,
            venta.Numero);

        _dbContext.StockMovimientos.Add(movimiento);

        var movimientoItems = new List<StockMovimientoItem>();
        var stockCambios = new List<StockSaldoChangeDto>();
        foreach (var requirement in requirements)
        {
            var saldo = saldoByProduct[requirement.Key];
            var before = saldo.CantidadActual;
            var after = before + requirement.Value;
            saldo.SetCantidad(after, nowUtc);

            var itemId = Guid.NewGuid();
            movimientoItems.Add(new StockMovimientoItem(
                itemId,
                tenantId,
                movimientoId,
                requirement.Key,
                requirement.Value,
                true,
                nowUtc));

            stockCambios.Add(new StockSaldoChangeDto(
                movimientoId,
                itemId,
                requirement.Key,
                before,
                after));
        }

        _dbContext.StockMovimientoItems.AddRange(movimientoItems);

        var movimientosSum = (await _dbContext.CajaMovimientos
            .Where(m => m.TenantId == tenantId && m.CajaSesionId == cajaSesion.Id)
            .Select(m => m.Monto)
            .ToListAsync(cancellationToken))
            .Sum();

        var saldoActual = cajaSesion.MontoInicial + movimientosSum;
        var cajaMovimientos = new List<CajaMovimientoResultDto>();

        foreach (var pago in pagos)
        {
            var saldoAntes = saldoActual;
            var monto = -pago.Monto;
            saldoActual += monto;

            var movimientoCaja = new CajaMovimiento(
                Guid.NewGuid(),
                tenantId,
                cajaSesion.Id,
                CajaMovimientoTipo.Egreso,
                pago.MedioPago,
                monto,
                $"Anulacion venta {ventaId} - {request.Motivo}",
                nowUtc,
                nowUtc);

            _dbContext.CajaMovimientos.Add(movimientoCaja);

            var dto = new CajaMovimientoDto(
                movimientoCaja.Id,
                movimientoCaja.CajaSesionId,
                movimientoCaja.Tipo.ToString().ToUpperInvariant(),
                movimientoCaja.MedioPago,
                movimientoCaja.Monto,
                movimientoCaja.Motivo,
                movimientoCaja.Fecha);

            cajaMovimientos.Add(new CajaMovimientoResultDto(dto, saldoAntes, saldoActual));
        }

        venta.CambiarEstado(VentaEstado.Anulada, nowUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var ventaDto = await GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken)
            ?? throw new NotFoundException("Venta no encontrada.");

        return new VentaAnularResultDto(
            ventaDto,
            stockCambios,
            cajaMovimientos);
    }

    public async Task<VentaPricingSnapshotDto?> GetPricingSnapshotAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid ventaId,
        CancellationToken cancellationToken = default)
    {
        var venta = await _dbContext.Ventas.AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.SucursalId == sucursalId && v.Id == ventaId, cancellationToken);

        if (venta is null)
        {
            return null;
        }

        var items = await (from i in _dbContext.VentaItems.AsNoTracking()
                join p in _dbContext.Productos.AsNoTracking() on i.ProductoId equals p.Id
                where i.TenantId == tenantId && i.VentaId == ventaId && p.TenantId == tenantId
                orderby p.Name
                select new VentaPricingItemDto(
                    i.Id,
                    i.ProductoId,
                    p.CategoriaId,
                    p.Name,
                    p.Sku,
                    i.Cantidad,
                    i.PrecioUnitario))
            .ToListAsync(cancellationToken);

        return new VentaPricingSnapshotDto(
            venta.Id,
            venta.ListaPrecio,
            items);
    }

    private async Task<decimal> GetPrecioUnitarioAsync(
        Guid tenantId,
        string listaPrecio,
        Guid productoId,
        decimal precioVenta,
        decimal precioBase,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(listaPrecio))
        {
            var lista = await _dbContext.ListasPrecio.AsNoTracking()
                .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Nombre == listaPrecio, cancellationToken);

            if (lista is not null)
            {
                var item = await _dbContext.ListaPrecioItems.AsNoTracking()
                    .Where(i => i.TenantId == tenantId && i.ListaPrecioId == lista.Id && i.ProductoId == productoId)
                    .Select(i => new { i.Precio })
                    .FirstOrDefaultAsync(cancellationToken);

                if (item is not null)
                {
                    return item.Precio;
                }
            }
        }

        return precioVenta > 0 ? precioVenta : precioBase;
    }

    private async Task EnsureStockDisponibleParaAgregarAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid productoId,
        string nombreProducto,
        bool esCombo,
        string? comboItemsJson,
        decimal cantidadSolicitada,
        CancellationToken cancellationToken)
    {
        var requirements = BuildStockRequirements(
            new[] { new ProductStockDefinition(productoId, esCombo, comboItemsJson) },
            new[] { new StockDemandItem(productoId, cantidadSolicitada) });

        var componentNames = requirements.Count > 1 || esCombo
            ? await _dbContext.Productos.AsNoTracking()
                .Where(p => p.TenantId == tenantId && requirements.Keys.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => $"{p.Name} ({p.Sku})", cancellationToken)
            : new Dictionary<Guid, string>();

        var faltantes = new List<string>();

        foreach (var requirement in requirements)
        {
            var disponible = await _dbContext.StockSaldos.AsNoTracking()
                .Where(s => s.TenantId == tenantId && s.SucursalId == sucursalId && s.ProductoId == requirement.Key)
                .Select(s => s.CantidadActual)
                .FirstOrDefaultAsync(cancellationToken);

            if (requirement.Value <= disponible)
            {
                continue;
            }

            var faltante = requirement.Value - disponible;
            var missingProductLabel = requirement.Key == productoId && !esCombo
                ? nombreProducto
                : componentNames.TryGetValue(requirement.Key, out var componentLabel)
                    ? componentLabel
                    : "producto componente";
            faltantes.Add($"{missingProductLabel} [Disponible: {disponible}. Solicitado: {requirement.Value}. Faltante: {faltante}.]");
        }

        if (faltantes.Count > 0)
        {
            throw new ConflictException(
                esCombo
                    ? $"Stock insuficiente para armar el combo {nombreProducto}. Faltantes: {string.Join(" | ", faltantes)}."
                    : $"Stock insuficiente para {faltantes[0]}");
        }
    }

    private static IReadOnlyDictionary<Guid, decimal> BuildStockRequirements(
        IEnumerable<ProductStockDefinition> products,
        IEnumerable<StockDemandItem> items)
    {
        var productMap = products.ToDictionary(
            p => p.ProductoId,
            p => new
            {
                p.IsCombo,
                p.ComboItemsJson
            });

        var requirements = new Dictionary<Guid, decimal>();

        foreach (var item in items)
        {
            var productId = item.ProductoId;
            var quantity = item.Cantidad;

            if (!productMap.TryGetValue(productId, out var productInfo))
            {
                continue;
            }

            if (!productInfo.IsCombo)
            {
                requirements[productId] = requirements.TryGetValue(productId, out var current)
                    ? current + quantity
                    : quantity;
                continue;
            }

            var comboItems = DeserializeComboItems(productInfo.ComboItemsJson);
            if (comboItems is null || comboItems.Count == 0)
            {
                requirements[productId] = requirements.TryGetValue(productId, out var currentCombo)
                    ? currentCombo + quantity
                    : quantity;
                continue;
            }

            foreach (var comboItem in comboItems)
            {
                var requiredQuantity = comboItem.Cantidad * quantity;
                requirements[comboItem.ProductoId] = requirements.TryGetValue(comboItem.ProductoId, out var currentRequirement)
                    ? currentRequirement + requiredQuantity
                    : requiredQuantity;
            }
        }

        return requirements;
    }

    private static IReadOnlyCollection<ProductoComboItemDto>? DeserializeComboItems(string? comboItemsJson)
    {
        if (string.IsNullOrWhiteSpace(comboItemsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<ProductoComboItemDto>>(comboItemsJson, ComboJsonOptions);
        }
        catch
        {
            return null;
        }
    }
}


