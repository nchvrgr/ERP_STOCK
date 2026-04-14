using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Ventas;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Ventas;

public sealed class VentaService
{
    private const string ListaPrecioDefault = "Minorista";

    private readonly IVentaRepository _repositorioVenta;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public VentaService(
        IVentaRepository repositorioVenta,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioVenta = repositorioVenta;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<VentaDto> IniciarVentaAsync(CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        var userId = AsegurarUsuario();

        var ventaId = await _repositorioVenta.CreateAsync(
            tenantId,
            sucursalId,
            userId,
            ListaPrecioDefault,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var venta = await _repositorioVenta.GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken);
        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "Venta",
            venta.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(venta),
            null,
            cancellationToken);

        return venta;
    }

    public async Task<VentaItemDto> AgregarItemPorCodigoAsync(
        Guid ventaId,
        VentaScanRequestDto request,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["code"] = new[] { "El SKU es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        var code = request.Code.Trim();

        var change = await _repositorioVenta.AddItemByCodeAsync(
            tenantId,
            sucursalId,
            ventaId,
            code,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "VentaItem",
            change.Item.Id.ToString(),
            change.Creado ? AuditAction.Create : AuditAction.Update,
            JsonSerializer.Serialize(new { cantidad = change.CantidadAntes }),
            JsonSerializer.Serialize(new { cantidad = change.CantidadDespues }),
            JsonSerializer.Serialize(new { ventaId }),
            cancellationToken);

        return change.Item;
    }

    public async Task<VentaItemDto> AgregarItemPorProductoAsync(
        Guid ventaId,
        VentaItemByProductRequestDto request,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (request is null || request.ProductId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var change = await _repositorioVenta.AddItemByProductAsync(
            tenantId,
            sucursalId,
            ventaId,
            request.ProductId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "VentaItem",
            change.Item.Id.ToString(),
            change.Creado ? AuditAction.Create : AuditAction.Update,
            JsonSerializer.Serialize(new { cantidad = change.CantidadAntes }),
            JsonSerializer.Serialize(new { cantidad = change.CantidadDespues }),
            JsonSerializer.Serialize(new { ventaId }),
            cancellationToken);

        return change.Item;
    }

    public async Task<VentaItemDto> ActualizarItemAsync(
        Guid ventaId,
        Guid itemId,
        VentaItemUpdateDto request,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (itemId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["itemId"] = new[] { "El item es obligatorio." }
                });
        }

        if (request is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        if (request.Cantidad < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["cantidad"] = new[] { "La cantidad debe ser mayor a 0." }
                });
        }

        if (request.PrecioUnitario.HasValue && request.PrecioUnitario.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["precioUnitario"] = new[] { "El precio unitario no puede ser negativo." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        if (request.Cantidad == 0)
        {
            var removed = await _repositorioVenta.RemoveItemAsync(
                tenantId,
                sucursalId,
                ventaId,
                itemId,
                DateTimeOffset.UtcNow,
                cancellationToken);

            await _servicioAuditoria.LogAsync(
                "VentaItem",
                removed.Id.ToString(),
                AuditAction.Delete,
                JsonSerializer.Serialize(new { cantidad = removed.Cantidad }),
                null,
                JsonSerializer.Serialize(new { ventaId }),
                cancellationToken);

            return removed;
        }

        var change = await _repositorioVenta.UpdateItemCantidadAsync(
            tenantId,
            sucursalId,
            ventaId,
            itemId,
            request.Cantidad,
            request.PrecioUnitario,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "VentaItem",
            change.Item.Id.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(new { cantidad = change.CantidadAntes }),
            JsonSerializer.Serialize(new { cantidad = change.CantidadDespues }),
            JsonSerializer.Serialize(new { ventaId }),
            cancellationToken);

        return change.Item;
    }

    public async Task<VentaItemDto> QuitarItemAsync(
        Guid ventaId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (itemId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["itemId"] = new[] { "El item es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var removed = await _repositorioVenta.RemoveItemAsync(
            tenantId,
            sucursalId,
            ventaId,
            itemId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "VentaItem",
            removed.Id.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(new { cantidad = removed.Cantidad }),
            null,
            JsonSerializer.Serialize(new { ventaId }),
            cancellationToken);

        return removed;
    }

    public async Task<VentaDto> ObtenerPorIdAsync(Guid ventaId, CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var venta = await _repositorioVenta.GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken);
        if (venta is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        return venta;
    }

    public async Task<VentaTicketDto> ObtenerTicketPorNumeroAsync(long numeroVenta, CancellationToken cancellationToken)
    {
        if (numeroVenta <= 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["numeroVenta"] = new[] { "El numero de venta es invalido." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var ticket = await _repositorioVenta.GetTicketByNumeroAsync(tenantId, sucursalId, numeroVenta, cancellationToken);
        if (ticket is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        return ticket;
    }

    public async Task<VentaConfirmResultDto> ConfirmarAsync(
        Guid ventaId,
        VentaConfirmRequestDto request,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (request is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        if (request.CajaSesionId.HasValue && request.CajaSesionId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["cajaSesionId"] = new[] { "La sesion de caja es invalida." }
                });
        }

        if (!request.Facturada.HasValue)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["facturada"] = new[] { "Debes indicar si la venta es facturada o no facturada." }
                });
        }

        var pagos = request.Pagos ?? Array.Empty<VentaPagoRequestDto>();
        foreach (var pago in pagos)
        {
            if (string.IsNullOrWhiteSpace(pago.MedioPago))
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["pagos"] = new[] { "El medio de pago es obligatorio." }
                    });
            }

            if (pago.Monto <= 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["pagos"] = new[] { "El monto debe ser mayor a 0." }
                    });
            }
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var before = await _repositorioVenta.GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken);
        if (before is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        var normalized = request with
        {
            Pagos = pagos
                .Select(p => new VentaPagoRequestDto(p.MedioPago.Trim().ToUpperInvariant(), p.Monto))
                .ToArray()
        };

        if (normalized.Facturada == true)
        {
            var tipoFactura = string.IsNullOrWhiteSpace(normalized.TipoFactura)
                ? "B"
                : normalized.TipoFactura.Trim().ToUpperInvariant();

            if (tipoFactura != "A" && tipoFactura != "B")
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["tipoFactura"] = new[] { "El tipo de factura debe ser A o B." }
                    });
            }

            var cliente = normalized.Cliente;
            if (tipoFactura == "A")
            {
                if (cliente is null || string.IsNullOrWhiteSpace(cliente.Nombre) || string.IsNullOrWhiteSpace(cliente.Cuit))
                {
                    throw new ValidationException(
                        "Validacion fallida.",
                        new Dictionary<string, string[]>
                        {
                            ["cliente"] = new[] { "Para Factura A se requiere nombre y CUIT del cliente." }
                        });
                }
            }

            normalized = normalized with
            {
                TipoFactura = tipoFactura,
                Cliente = cliente is null
                    ? null
                    : new VentaClienteFacturaDto(
                        cliente.Nombre?.Trim() ?? string.Empty,
                        string.IsNullOrWhiteSpace(cliente.Cuit) ? null : cliente.Cuit.Trim(),
                        string.IsNullOrWhiteSpace(cliente.Direccion) ? null : cliente.Direccion.Trim(),
                        string.IsNullOrWhiteSpace(cliente.Telefono) ? null : cliente.Telefono.Trim())
            };
        }
        else
        {
            normalized = normalized with
            {
                TipoFactura = null,
                Cliente = null
            };
        }

        var result = await _repositorioVenta.ConfirmAsync(
            tenantId,
            sucursalId,
            ventaId,
            normalized,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "Venta",
            ventaId.ToString(),
            AuditAction.Confirm,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(result.Venta),
            null,
            cancellationToken);

        foreach (var cambio in result.StockCambios)
        {
            await _servicioAuditoria.LogAsync(
                "StockSaldo",
                $"{cambio.ProductoId}:{sucursalId}",
                AuditAction.Adjust,
                JsonSerializer.Serialize(new { cantidadActual = cambio.SaldoAntes }),
                JsonSerializer.Serialize(new { cantidadActual = cambio.SaldoDespues }),
                JsonSerializer.Serialize(new { movimientoId = cambio.MovimientoId, itemId = cambio.MovimientoItemId, ventaId }),
                cancellationToken);
        }

        foreach (var movimiento in result.CajaMovimientos)
        {
            await _servicioAuditoria.LogAsync(
                "CajaMovimiento",
                movimiento.Movimiento.Id.ToString(),
                AuditAction.Adjust,
                JsonSerializer.Serialize(new { saldo = movimiento.SaldoAntes }),
                JsonSerializer.Serialize(new { saldo = movimiento.SaldoDespues }),
                JsonSerializer.Serialize(new { ventaId }),
                cancellationToken);
        }

        return result;
    }

    public async Task<VentaAnularResultDto> AnularAsync(
        Guid ventaId,
        VentaAnularRequestDto request,
        CancellationToken cancellationToken)
    {
        if (ventaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ventaId"] = new[] { "La venta es obligatoria." }
                });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Motivo))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["motivo"] = new[] { "El motivo es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var before = await _repositorioVenta.GetByIdAsync(tenantId, sucursalId, ventaId, cancellationToken);
        if (before is null)
        {
            throw new NotFoundException("Venta no encontrada.");
        }

        var normalized = request with { Motivo = request.Motivo.Trim() };

        var result = await _repositorioVenta.AnularAsync(
            tenantId,
            sucursalId,
            ventaId,
            normalized,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "Venta",
            ventaId.ToString(),
            AuditAction.Cancel,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(result.Venta),
            JsonSerializer.Serialize(new { motivo = normalized.Motivo }),
            cancellationToken);

        foreach (var cambio in result.StockCambios)
        {
            await _servicioAuditoria.LogAsync(
                "StockSaldo",
                $"{cambio.ProductoId}:{sucursalId}",
                AuditAction.Adjust,
                JsonSerializer.Serialize(new { cantidadActual = cambio.SaldoAntes }),
                JsonSerializer.Serialize(new { cantidadActual = cambio.SaldoDespues }),
                JsonSerializer.Serialize(new { movimientoId = cambio.MovimientoId, itemId = cambio.MovimientoItemId, ventaId }),
                cancellationToken);
        }

        foreach (var movimiento in result.CajaMovimientos)
        {
            await _servicioAuditoria.LogAsync(
                "CajaMovimiento",
                movimiento.Movimiento.Id.ToString(),
                AuditAction.Adjust,
                JsonSerializer.Serialize(new { saldo = movimiento.SaldoAntes }),
                JsonSerializer.Serialize(new { saldo = movimiento.SaldoDespues }),
                JsonSerializer.Serialize(new { ventaId }),
                cancellationToken);
        }

        return result;
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

    private Guid AsegurarUsuario()
    {
        if (_contextoSolicitud.UserId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de usuario invalido.");
        }

        return _contextoSolicitud.UserId;
    }
}


