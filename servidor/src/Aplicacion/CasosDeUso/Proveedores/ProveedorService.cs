using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Proveedores;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Proveedores;

public sealed class ProveedorService
{
    private readonly IProveedorRepository _repositorioProveedor;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public ProveedorService(
        IProveedorRepository repositorioProveedor,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioProveedor = repositorioProveedor;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<IReadOnlyList<ProveedorDto>> BuscarAsync(
        string? busqueda,
        bool? activo,
        CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        return await _repositorioProveedor.SearchAsync(tenantId, busqueda, activo, cancellationToken);
    }

    public async Task<ProveedorDto> CrearAsync(ProveedorCreateDto solicitud, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(solicitud.Name))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "El nombre es obligatorio." }
                });
        }

        if (string.IsNullOrWhiteSpace(solicitud.Telefono))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["telefono"] = new[] { "El telefono es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var now = DateTimeOffset.UtcNow;
        var normalizado = solicitud with
        {
            Name = solicitud.Name.Trim(),
            Telefono = solicitud.Telefono.Trim(),
            Cuit = string.IsNullOrWhiteSpace(solicitud.Cuit) ? null : solicitud.Cuit.Trim(),
            Direccion = string.IsNullOrWhiteSpace(solicitud.Direccion) ? null : solicitud.Direccion.Trim()
        };

        var id = await _repositorioProveedor.CreateAsync(tenantId, normalizado, now, cancellationToken);
        var proveedor = await _repositorioProveedor.GetByIdAsync(tenantId, id, cancellationToken);
        if (proveedor is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Proveedor",
            proveedor.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(proveedor),
            null,
            cancellationToken);

        return proveedor;
    }

    public async Task<ProveedorDto> ActualizarAsync(Guid proveedorId, ProveedorUpdateDto solicitud, CancellationToken cancellationToken)
    {
        if (proveedorId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        if (solicitud is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        var hayCambios = solicitud.Name is not null
            || solicitud.Telefono is not null
            || solicitud.Cuit is not null
            || solicitud.Direccion is not null
            || solicitud.IsActive is not null;
        if (!hayCambios)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "Debe enviar al menos un cambio." }
                });
        }

        if (solicitud.Name is not null && string.IsNullOrWhiteSpace(solicitud.Name))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "El nombre no puede estar vacio." }
                });
        }

        if (solicitud.Telefono is not null && string.IsNullOrWhiteSpace(solicitud.Telefono))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["telefono"] = new[] { "El telefono no puede estar vacio." }
                });
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioProveedor.GetByIdAsync(tenantId, proveedorId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        var normalizado = solicitud with
        {
            Name = solicitud.Name?.Trim(),
            Telefono = solicitud.Telefono?.Trim(),
            Cuit = solicitud.Cuit is null ? null : string.IsNullOrWhiteSpace(solicitud.Cuit) ? null : solicitud.Cuit.Trim(),
            Direccion = solicitud.Direccion is null ? null : string.IsNullOrWhiteSpace(solicitud.Direccion) ? null : solicitud.Direccion.Trim()
        };
        var actualizado = await _repositorioProveedor.UpdateAsync(tenantId, proveedorId, normalizado, DateTimeOffset.UtcNow, cancellationToken);
        if (!actualizado)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        var despues = await _repositorioProveedor.GetByIdAsync(tenantId, proveedorId, cancellationToken);
        if (despues is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Proveedor",
            proveedorId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            null,
            cancellationToken);

        return despues;
    }

    public async Task EliminarAsync(Guid proveedorId, CancellationToken cancellationToken)
    {
        if (proveedorId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioProveedor.GetByIdAsync(tenantId, proveedorId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        var eliminado = await _repositorioProveedor.DeleteAsync(tenantId, proveedorId, cancellationToken);
        if (!eliminado)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Proveedor",
            proveedorId.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(antes),
            null,
            null,
            cancellationToken);
    }

    public async Task<ProveedorDeletePreviewDto> ObtenerVistaPreviaEliminacionAsync(
        Guid proveedorId,
        CancellationToken cancellationToken)
    {
        if (proveedorId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var proveedor = await _repositorioProveedor.GetByIdAsync(tenantId, proveedorId, cancellationToken);
        if (proveedor is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        var productos = await _repositorioProveedor.GetDeleteProductOptionsAsync(tenantId, proveedorId, cancellationToken);
        return new ProveedorDeletePreviewDto(proveedorId, proveedor.Name, productos);
    }

    public async Task EliminarConResolucionDeProductosAsync(
        Guid proveedorId,
        ProveedorDeleteRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        if (proveedorId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        if (solicitud is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioProveedor.GetByIdAsync(tenantId, proveedorId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        var eliminado = await _repositorioProveedor.DeleteWithProductResolutionAsync(
            tenantId,
            proveedorId,
            solicitud.ProductIdsToDelete ?? Array.Empty<Guid>(),
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (!eliminado)
        {
            throw new NotFoundException("Proveedor no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Proveedor",
            proveedorId.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(antes),
            null,
            JsonSerializer.Serialize(new
            {
                deletedProductIds = solicitud.ProductIdsToDelete
            }),
            cancellationToken);
    }

    private Guid AsegurarTenant()
    {
        if (_contextoSolicitud.TenantId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        return _contextoSolicitud.TenantId;
    }
}


