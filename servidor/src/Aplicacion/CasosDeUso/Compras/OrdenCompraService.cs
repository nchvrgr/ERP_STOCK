using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Compras;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Compras;

public sealed class OrdenCompraService
{
    private readonly IOrdenCompraRepository _repositorioOrdenCompra;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public OrdenCompraService(
        IOrdenCompraRepository repositorioOrdenCompra,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioOrdenCompra = repositorioOrdenCompra;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<OrdenCompraDto> CrearAsync(OrdenCompraCreateDto solicitud, CancellationToken cancellationToken)
    {
        if (solicitud is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        if (solicitud.ProveedorId.HasValue && solicitud.ProveedorId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es invalido." }
                });
        }

        if (solicitud.Items is null || solicitud.Items.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["items"] = new[] { "Debe incluir al menos un item." }
                });
        }

        var duplicados = solicitud.Items
            .GroupBy(i => i.ProductoId)
            .Where(g => g.Key != Guid.Empty && g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicados.Count > 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["items"] = new[] { "No se permiten productos duplicados." }
                });
        }

        foreach (var item in solicitud.Items)
        {
            if (item.ProductoId == Guid.Empty)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["productoId"] = new[] { "El producto es obligatorio." }
                    });
            }

            if (item.Cantidad <= 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["cantidad"] = new[] { "La cantidad debe ser mayor a 0." }
                    });
            }
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var ordenId = await _repositorioOrdenCompra.CreateAsync(
            tenantId,
            sucursalId,
            solicitud,
            DateTimeOffset.UtcNow,
            cancellationToken);

        var orden = await _repositorioOrdenCompra.GetByIdAsync(tenantId, sucursalId, ordenId, cancellationToken);
        if (orden is null)
        {
            throw new NotFoundException("Orden de compra no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "OrdenCompra",
            orden.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(orden),
            null,
            cancellationToken);

        return orden;
    }

    public async Task<IReadOnlyList<OrdenCompraListItemDto>> ObtenerListaAsync(CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        return await _repositorioOrdenCompra.GetListAsync(tenantId, sucursalId, cancellationToken);
    }

    public async Task<OrdenCompraDto> ObtenerPorIdAsync(Guid ordenCompraId, CancellationToken cancellationToken)
    {
        if (ordenCompraId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ordenCompraId"] = new[] { "La orden es obligatoria." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var orden = await _repositorioOrdenCompra.GetByIdAsync(tenantId, sucursalId, ordenCompraId, cancellationToken);
        if (orden is null)
        {
            throw new NotFoundException("Orden de compra no encontrada.");
        }

        return orden;
    }

    public async Task<OrdenCompraDto> EnviarAsync(Guid ordenCompraId, CancellationToken cancellationToken)
    {
        if (ordenCompraId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ordenCompraId"] = new[] { "La orden es obligatoria." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var antes = await _repositorioOrdenCompra.GetByIdAsync(tenantId, sucursalId, ordenCompraId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Orden de compra no encontrada.");
        }

        var despues = await _repositorioOrdenCompra.EnviarAsync(
            tenantId,
            sucursalId,
            ordenCompraId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "OrdenCompra",
            ordenCompraId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            null,
            cancellationToken);

        return despues;
    }

    public async Task<OrdenCompraDto> CancelarAsync(Guid ordenCompraId, CancellationToken cancellationToken)
    {
        if (ordenCompraId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["ordenCompraId"] = new[] { "La orden es obligatoria." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var antes = await _repositorioOrdenCompra.GetByIdAsync(tenantId, sucursalId, ordenCompraId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Orden de compra no encontrada.");
        }

        var despues = await _repositorioOrdenCompra.CancelarAsync(
            tenantId,
            sucursalId,
            ordenCompraId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "OrdenCompra",
            ordenCompraId.ToString(),
            AuditAction.Cancel,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            null,
            cancellationToken);

        return despues;
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


