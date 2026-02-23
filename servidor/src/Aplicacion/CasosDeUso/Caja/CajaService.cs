using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Caja;

public sealed class CajaService
{
    private readonly ICajaRepository _repositorioCaja;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public CajaService(
        ICajaRepository repositorioCaja,
        IRequestContext contextoSolicitud,
        IAuditLogService servicioAuditoria)
    {
        _repositorioCaja = repositorioCaja;
        _contextoSolicitud = contextoSolicitud;
        _servicioAuditoria = servicioAuditoria;
    }

    public async Task<IReadOnlyList<CajaDto>> ObtenerCajasAsync(
        bool? activo,
        CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        return await _repositorioCaja.GetCajasAsync(tenantId, sucursalId, activo, cancellationToken);
    }

    public async Task<CajaDto> CrearCajaAsync(CajaCreateDto solicitud, CancellationToken cancellationToken)
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

        if (string.IsNullOrWhiteSpace(solicitud.Nombre))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["nombre"] = new[] { "El nombre es obligatorio." }
                });
        }

        if (string.IsNullOrWhiteSpace(solicitud.Numero))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["numero"] = new[] { "El numero es obligatorio." }
                });
        }

        if (!solicitud.Numero.All(char.IsDigit))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["numero"] = new[] { "El numero debe contener solo digitos." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        var now = DateTimeOffset.UtcNow;

        var normalizada = solicitud with
        {
            Nombre = solicitud.Nombre.Trim(),
            Numero = solicitud.Numero.Trim()
        };

        var creada = await _repositorioCaja.CreateCajaAsync(tenantId, sucursalId, normalizada, now, cancellationToken);

        await _servicioAuditoria.LogAsync(
            "Caja",
            creada.Id.ToString(),
            AuditAction.Create,
            null,
            System.Text.Json.JsonSerializer.Serialize(creada),
            null,
            cancellationToken);

        return creada;
    }

    public async Task<CajaSesionDto> AbrirSesionAsync(CajaSesionAbrirDto solicitud, CancellationToken cancellationToken)
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

        if (solicitud.CajaId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["cajaId"] = new[] { "La caja es obligatoria." }
                });
        }

        if (solicitud.MontoInicial < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["montoInicial"] = new[] { "El monto inicial debe ser mayor o igual a 0." }
                });
        }

        if (string.IsNullOrWhiteSpace(solicitud.Turno))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["turno"] = new[] { "El turno es obligatorio." }
                });
        }

        var turno = solicitud.Turno.Trim().ToUpperInvariant();
        var turnosValidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "MANANA", "TARDE", "NOCHE" };
        if (!turnosValidos.Contains(turno))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["turno"] = new[] { "Turno invalido. Usa MANANA, TARDE o NOCHE." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var existe = await _repositorioCaja.CajaExistsAsync(tenantId, sucursalId, solicitud.CajaId, cancellationToken);
        if (!existe)
        {
            throw new NotFoundException("Caja no encontrada.");
        }

        var tieneSesionAbierta = await _repositorioCaja.HasOpenSessionAsync(tenantId, solicitud.CajaId, cancellationToken);
        if (tieneSesionAbierta)
        {
            throw new ConflictException("Ya existe una sesion abierta para esta caja.");
        }

        var sesion = await _repositorioCaja.OpenSessionAsync(
            tenantId,
            sucursalId,
            solicitud.CajaId,
            solicitud.MontoInicial,
            turno,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "CajaSesion",
            sesion.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(sesion),
            null,
            cancellationToken);

        return sesion;
    }

    public async Task<CajaMovimientoDto> RegistrarMovimientoAsync(
        Guid sesionId,
        CajaMovimientoCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        if (sesionId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sesionId"] = new[] { "La sesion es obligatoria." }
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

        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["motivo"] = new[] { "El motivo es obligatorio." }
                });
        }

        if (!Enum.TryParse<CajaMovimientoTipo>(solicitud.Tipo, true, out var tipo))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["tipo"] = new[] { "Tipo de movimiento invalido." }
                });
        }

        if (solicitud.Monto == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["monto"] = new[] { "El monto no puede ser 0." }
                });
        }

        decimal montoSigned;
        switch (tipo)
        {
            case CajaMovimientoTipo.Retiro:
            case CajaMovimientoTipo.Gasto:
                if (solicitud.Monto <= 0)
                {
                    throw new ValidationException(
                        "Validacion fallida.",
                        new Dictionary<string, string[]>
                        {
                            ["monto"] = new[] { "El monto debe ser mayor a 0." }
                        });
                }
                montoSigned = -Math.Abs(solicitud.Monto);
                break;
            case CajaMovimientoTipo.Ajuste:
                montoSigned = solicitud.Monto;
                break;
            default:
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["tipo"] = new[] { "Tipo de movimiento invalido." }
                    });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        var medioPago = string.IsNullOrWhiteSpace(solicitud.MedioPago)
            ? "EFECTIVO"
            : solicitud.MedioPago.Trim().ToUpperInvariant();

        var resultado = await _repositorioCaja.AddMovimientoAsync(
            tenantId,
            sucursalId,
            sesionId,
            tipo,
            montoSigned,
            solicitud.Motivo.Trim(),
            medioPago,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "CajaMovimiento",
            resultado.Movimiento.Id.ToString(),
            AuditAction.Adjust,
            JsonSerializer.Serialize(new { saldo = resultado.SaldoAntes }),
            JsonSerializer.Serialize(new { saldo = resultado.SaldoDespues }),
            JsonSerializer.Serialize(new { cajaSesionId = sesionId }),
            cancellationToken);

        return resultado.Movimiento;
    }

    public async Task<CajaCierreResultDto> CerrarSesionAsync(
        Guid sesionId,
        CajaCierreRequestDto solicitud,
        CancellationToken cancellationToken)
    {
        if (sesionId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sesionId"] = new[] { "La sesion es obligatoria." }
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

        if (solicitud.EfectivoContado < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["efectivoContado"] = new[] { "El efectivo contado no puede ser negativo." }
                });
        }

        var medios = solicitud.Medios ?? Array.Empty<CajaCierreMedioDto>();
        foreach (var medio in medios)
        {
            if (string.IsNullOrWhiteSpace(medio.Medio))
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["medios"] = new[] { "El medio de pago es obligatorio." }
                    });
            }

            if (medio.Contado < 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["medios"] = new[] { "El monto contado no puede ser negativo." }
                    });
            }

            if (string.Equals(medio.Medio.Trim(), "EFECTIVO", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["medios"] = new[] { "El efectivo se informa en el campo efectivoContado." }
                    });
            }
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        var anterior = await _repositorioCaja.GetSesionAsync(tenantId, sucursalId, sesionId, cancellationToken);
        if (anterior is null)
        {
            throw new NotFoundException("Sesion no encontrada.");
        }

        var normalizada = solicitud with
        {
            Medios = medios
                .Select(m => new CajaCierreMedioDto(m.Medio.Trim().ToUpperInvariant(), m.Contado))
                .ToArray()
        };

        var resultado = await _repositorioCaja.CloseSessionAsync(
            tenantId,
            sucursalId,
            sesionId,
            normalizada,
            DateTimeOffset.UtcNow,
            cancellationToken);

        await _servicioAuditoria.LogAsync(
            "CajaSesion",
            sesionId.ToString(),
            AuditAction.Close,
            JsonSerializer.Serialize(anterior),
            JsonSerializer.Serialize(resultado),
            JsonSerializer.Serialize(new { sesionId }),
            cancellationToken);

        return resultado;
    }

    public async Task<CajaResumenDto> ObtenerResumenAsync(Guid sesionId, CancellationToken cancellationToken)
    {
        if (sesionId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sesionId"] = new[] { "La sesion es obligatoria." }
                });
        }

        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();
        var resumen = await _repositorioCaja.GetResumenAsync(tenantId, sucursalId, sesionId, cancellationToken);
        if (resumen is null)
        {
            throw new NotFoundException("Sesion no encontrada.");
        }

        return resumen;
    }

    public async Task<IReadOnlyList<CajaHistorialDto>> ObtenerHistorialAsync(
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken)
    {
        var tenantId = AsegurarTenant();
        var sucursalId = AsegurarSucursal();

        // El filtro de fecha en UI es fecha local (Argentina). Convertimos ese rango local a UTC.
        var appOffset = TimeSpan.FromHours(-3);
        DateTimeOffset? desdeUtc = desde.HasValue
            ? new DateTimeOffset(desde.Value.ToDateTime(TimeOnly.MinValue), appOffset).ToUniversalTime()
            : null;
        DateTimeOffset? hastaUtc = hasta.HasValue
            ? new DateTimeOffset(hasta.Value.ToDateTime(TimeOnly.MaxValue), appOffset).ToUniversalTime()
            : null;

        if (desdeUtc.HasValue && hastaUtc.HasValue && desdeUtc > hastaUtc)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["fecha"] = new[] { "Rango de fechas invalido." }
                });
        }

        return await _repositorioCaja.GetSesionesHistoricasAsync(tenantId, sucursalId, desdeUtc, hastaUtc, cancellationToken);
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


