using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.DescuentosRecargos;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.DescuentosRecargos;

public sealed class DescuentoRecargoService
{
    private readonly IDescuentoRecargoRepository _repositorio;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public DescuentoRecargoService(
        IDescuentoRecargoRepository repositorio,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorio = repositorio;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<IReadOnlyList<DescuentoRecargoDto>> BuscarAsync(
        string? tipo,
        string? busqueda,
        CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        return await _repositorio.SearchAsync(tenantId, ParseTipoOptional(tipo), busqueda, cancellationToken);
    }

    public async Task<DescuentoRecargoDto> CrearAsync(
        DescuentoRecargoCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        if (solicitud is null)
        {
            throw ValidationError("request", "El request es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.Name))
        {
            throw ValidationError("name", "El nombre es obligatorio.");
        }

        if (!solicitud.Porcentaje.HasValue || solicitud.Porcentaje.Value <= 0 || solicitud.Porcentaje.Value > 100)
        {
            throw ValidationError("porcentaje", "El porcentaje debe ser mayor a 0 y menor o igual a 100.");
        }

        var tipo = ParseTipo(solicitud.Tipo, "tipo");
        var tenantId = AsegurarTenant();
        var normalizada = solicitud with { Name = solicitud.Name.Trim(), Tipo = tipo.ToString().ToUpperInvariant() };
        var id = await _repositorio.CreateAsync(tenantId, normalizada, tipo, DateTimeOffset.UtcNow, cancellationToken);
        var creada = await _repositorio.GetByIdAsync(tenantId, id, cancellationToken);
        if (creada is null)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "DescuentoRecargo",
            creada.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(creada),
            null,
            cancellationToken);

        return creada;
    }

    public async Task<DescuentoRecargoDto> ActualizarAsync(
        Guid descuentoRecargoId,
        DescuentoRecargoUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        if (descuentoRecargoId == Guid.Empty)
        {
            throw ValidationError("descuentoRecargoId", "El elemento es obligatorio.");
        }

        if (solicitud is null)
        {
            throw ValidationError("request", "El request es obligatorio.");
        }

        var hayCambios = solicitud.Name is not null || solicitud.Porcentaje is not null;
        if (!hayCambios)
        {
            throw ValidationError("request", "Debe enviar al menos un cambio.");
        }

        if (solicitud.Name is not null && string.IsNullOrWhiteSpace(solicitud.Name))
        {
            throw ValidationError("name", "El nombre no puede estar vacio.");
        }

        if (solicitud.Porcentaje.HasValue && (solicitud.Porcentaje.Value <= 0 || solicitud.Porcentaje.Value > 100))
        {
            throw ValidationError("porcentaje", "El porcentaje debe ser mayor a 0 y menor o igual a 100.");
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorio.GetByIdAsync(tenantId, descuentoRecargoId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        var normalizada = solicitud with { Name = solicitud.Name?.Trim() };
        var actualizada = await _repositorio.UpdateAsync(tenantId, descuentoRecargoId, normalizada, DateTimeOffset.UtcNow, cancellationToken);
        if (!actualizada)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        var despues = await _repositorio.GetByIdAsync(tenantId, descuentoRecargoId, cancellationToken);
        if (despues is null)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "DescuentoRecargo",
            descuentoRecargoId.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(antes),
            JsonSerializer.Serialize(despues),
            null,
            cancellationToken);

        return despues;
    }

    public async Task EliminarAsync(Guid descuentoRecargoId, CancellationToken cancellationToken)
    {
        if (descuentoRecargoId == Guid.Empty)
        {
            throw ValidationError("descuentoRecargoId", "El elemento es obligatorio.");
        }

        var tenantId = AsegurarTenant();
        var antes = await _repositorio.GetByIdAsync(tenantId, descuentoRecargoId, cancellationToken);
        if (antes is null)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        var eliminado = await _repositorio.DeleteAsync(tenantId, descuentoRecargoId, cancellationToken);
        if (!eliminado)
        {
            throw new NotFoundException("Descuento/recargo no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "DescuentoRecargo",
            descuentoRecargoId.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(antes),
            null,
            null,
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

    private static ValidationException ValidationError(string key, string message)
    {
        return new ValidationException(
            "Validacion fallida.",
            new Dictionary<string, string[]>
            {
                [key] = new[] { message }
            });
    }

    private static DescuentoRecargoTipo ParseTipo(string? raw, string fieldName)
    {
        if (!TryParseTipo(raw, out var tipo))
        {
            throw ValidationError(fieldName, "El tipo debe ser DESCUENTO o RECARGO.");
        }

        return tipo;
    }

    private static DescuentoRecargoTipo? ParseTipoOptional(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return ParseTipo(raw, "tipo");
    }

    private static bool TryParseTipo(string? raw, out DescuentoRecargoTipo tipo)
    {
        tipo = DescuentoRecargoTipo.Descuento;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = raw.Trim().ToUpperInvariant();
        return normalized switch
        {
            "DESCUENTO" => SetTipo(DescuentoRecargoTipo.Descuento, out tipo),
            "RECARGO" => SetTipo(DescuentoRecargoTipo.Recargo, out tipo),
            _ => false
        };
    }

    private static bool SetTipo(DescuentoRecargoTipo value, out DescuentoRecargoTipo tipo)
    {
        tipo = value;
        return true;
    }
}
