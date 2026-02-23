using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Categorias;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Categorias;

public sealed class CategoriaPrecioService
{
    private readonly ICategoriaPrecioRepository _repositorioCategoria;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public CategoriaPrecioService(
        ICategoriaPrecioRepository repositorioCategoria,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioCategoria = repositorioCategoria;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<IReadOnlyList<CategoriaPrecioDto>> BuscarAsync(
        string? busqueda,
        bool? activo,
        CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        return await _repositorioCategoria.SearchAsync(tenantId, busqueda, activo, cancellationToken);
    }

    public async Task<CategoriaPrecioDto> CrearAsync(CategoriaPrecioCreateDto solicitud, CancellationToken cancellationToken)
    {
        if (solicitud is null || string.IsNullOrWhiteSpace(solicitud.Name))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "El nombre es obligatorio." }
                });
        }

        if (solicitud.MargenGananciaPct.HasValue && solicitud.MargenGananciaPct.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["margenGananciaPct"] = new[] { "El margen no puede ser negativo." }
                });
        }

        var tenantId = AsegurarTenant();
        var normalizada = solicitud with { Name = solicitud.Name.Trim() };
        var id = await _repositorioCategoria.CreateAsync(tenantId, normalizada, DateTimeOffset.UtcNow, cancellationToken);
        var creada = await _repositorioCategoria.GetByIdAsync(tenantId, id, cancellationToken);
        if (creada is null)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "Categoria",
            creada.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(creada),
            null,
            cancellationToken);

        return creada;
    }

    public async Task<CategoriaPrecioDto> ActualizarAsync(
        Guid categoriaId,
        CategoriaPrecioUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        if (categoriaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["categoriaId"] = new[] { "La categoria es obligatoria." }
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
            || solicitud.MargenGananciaPct is not null
            || solicitud.IsActive is not null
            || solicitud.AplicarAProductos is not null;

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

        if (solicitud.MargenGananciaPct.HasValue && solicitud.MargenGananciaPct.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["margenGananciaPct"] = new[] { "El margen no puede ser negativo." }
                });
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorioCategoria.GetByIdAsync(tenantId, categoriaId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }

        var normalizada = solicitud with { Name = solicitud.Name?.Trim() };
        var actualizada = await _repositorioCategoria.UpdateAsync(tenantId, categoriaId, normalizada, DateTimeOffset.UtcNow, cancellationToken);
        if (!actualizada)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }

        var despues = await _repositorioCategoria.GetByIdAsync(tenantId, categoriaId, cancellationToken);
        if (despues is null)
        {
            throw new NotFoundException("Categoria no encontrada.");
        }

        await _servicioAuditoria.LogAsync(
            "Categoria",
            categoriaId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            JsonSerializer.Serialize(new { aplicarAProductos = solicitud.AplicarAProductos ?? true }),
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
}


