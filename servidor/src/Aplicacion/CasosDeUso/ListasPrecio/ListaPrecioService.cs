using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.ListasPrecio;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.ListasPrecio;

public sealed class ListaPrecioService
{
    private readonly IListaPrecioRepository _repositorioListaPrecio;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public ListaPrecioService(
        IListaPrecioRepository repositorioListaPrecio,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioListaPrecio = repositorioListaPrecio;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<IReadOnlyList<ListaPrecioDto>> ObtenerListaAsync(CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        return await _repositorioListaPrecio.GetListAsync(tenantId, cancellationToken);
    }

    public async Task<ListaPrecioDto> CrearAsync(ListaPrecioCreateDto solicitud, CancellationToken cancellationToken)
    {
        if (solicitud is null || string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["nombre"] = new[] { "El nombre es obligatorio." }
                });
        }

        var tenantId = AsegurarTenant();
        var normalizada = solicitud with { Nombre = solicitud.Nombre.Trim() };
        var id = await _repositorioListaPrecio.CreateAsync(tenantId, normalizada, DateTimeOffset.UtcNow, cancellationToken);
        var creada = await _repositorioListaPrecio.GetByIdAsync(tenantId, id, cancellationToken);
        if (creada is null)
        {
            throw new NotFoundException("Lista de precio no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "ListaPrecio",
            creada.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(creada),
            null,
            cancellationToken);

        return creada;
    }

    public async Task<ListaPrecioDto> ActualizarAsync(Guid listaPrecioId, ListaPrecioUpdateDto solicitud, CancellationToken cancellationToken)
    {
        if (listaPrecioId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["listaPrecioId"] = new[] { "La lista es obligatoria." }
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

        var hayCambios = solicitud.Nombre is not null || solicitud.IsActive is not null;
        if (!hayCambios)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "Debe enviar al menos un cambio." }
                });
        }

        if (solicitud.Nombre is not null && string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["nombre"] = new[] { "El nombre no puede estar vacio." }
                });
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioListaPrecio.GetByIdAsync(tenantId, listaPrecioId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Lista de precio no encontrada.");
        }

        var normalizada = solicitud with { Nombre = solicitud.Nombre?.Trim() };
        var actualizada = await _repositorioListaPrecio.UpdateAsync(tenantId, listaPrecioId, normalizada, DateTimeOffset.UtcNow, cancellationToken);
        if (!actualizada)
        {
            throw new NotFoundException("Lista de precio no encontrada.");
        }

        var despues = await _repositorioListaPrecio.GetByIdAsync(tenantId, listaPrecioId, cancellationToken);
        if (despues is null)
        {
            throw new NotFoundException("Lista de precio no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "ListaPrecio",
            listaPrecioId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            null,
            cancellationToken);

        return despues;
    }

    public async Task ActualizarItemsAsync(Guid listaPrecioId, ListaPrecioItemsUpdateDto solicitud, CancellationToken cancellationToken)
    {
        if (listaPrecioId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["listaPrecioId"] = new[] { "La lista es obligatoria." }
                });
        }

        if (solicitud is null || solicitud.Items is null || solicitud.Items.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["items"] = new[] { "Debe incluir items." }
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

            if (item.Precio <= 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["precio"] = new[] { "El precio debe ser mayor a 0." }
                    });
            }
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioListaPrecio.GetByIdAsync(tenantId, listaPrecioId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Lista de precio no encontrada.");
        }

        await _repositorioListaPrecio.UpsertItemsAsync(
            tenantId,
            listaPrecioId,
            solicitud.Items,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "ListaPrecioItems",
            listaPrecioId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            null,
            JsonSerializer.Serialize(new { totalItems = solicitud.Items.Count }),
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


