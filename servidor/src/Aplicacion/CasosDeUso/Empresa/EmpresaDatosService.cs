using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Empresa;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Empresa;

public sealed class EmpresaDatosService
{
    private static readonly string[] DefaultMediosPago =
    {
        "EFECTIVO",
        "TARJETA",
        "TRANSFERENCIA",
        "APLICATIVO",
        "OTRO"
    };

    private readonly IEmpresaDatosRepository _empresaDatosRepository;
    private readonly IRequestContext _requestContext;
    private readonly IAuditLogService _auditLogService;

    public EmpresaDatosService(
        IEmpresaDatosRepository empresaDatosRepository,
        IRequestContext requestContext,
        IAuditLogService auditLogService)
    {
        _empresaDatosRepository = empresaDatosRepository;
        _requestContext = requestContext;
        _auditLogService = auditLogService;
    }

    public async Task<EmpresaDatosDto> GetAsync(CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var data = await _empresaDatosRepository.GetAsync(tenantId, cancellationToken);
        if (data is not null)
        {
            var mediosPago = NormalizeMediosPago(data.MediosPago);
            var medioPagoHabitual = NormalizeMedioPagoHabitual(data.MedioPagoHabitual, mediosPago);
            return data with
            {
                MedioPagoHabitual = medioPagoHabitual,
                MediosPago = mediosPago
            };
        }

        return new EmpresaDatosDto(
            Guid.Empty,
            string.Empty,
            null,
            null,
            null,
            null,
            null,
            null,
            "EFECTIVO",
            DefaultMediosPago);
    }

    public async Task<EmpresaDatosDto> UpsertAsync(EmpresaDatosUpsertDto request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["razonSocial"] = new[] { "La razon social es obligatoria." }
                });
        }

        var tenantId = EnsureTenant();
        var before = await _empresaDatosRepository.GetAsync(tenantId, cancellationToken);
        var normalizedMediosPago = NormalizeMediosPago(request.MediosPago);
        var normalizedMedioPagoHabitual = NormalizeMedioPagoHabitual(request.MedioPagoHabitual, normalizedMediosPago);

        var normalized = request with
        {
            RazonSocial = request.RazonSocial.Trim(),
            Cuit = NormalizeNullable(request.Cuit),
            Telefono = NormalizeNullable(request.Telefono),
            Direccion = NormalizeNullable(request.Direccion),
            Email = NormalizeNullable(request.Email),
            Web = NormalizeNullable(request.Web),
            Observaciones = NormalizeNullable(request.Observaciones),
            MedioPagoHabitual = normalizedMedioPagoHabitual,
            MediosPago = normalizedMediosPago
        };

        var saved = await _empresaDatosRepository.UpsertAsync(tenantId, normalized, DateTimeOffset.UtcNow, cancellationToken);

        await _auditLogService.LogAsync(
            "EmpresaDatos",
            saved.Id == Guid.Empty ? tenantId.ToString() : saved.Id.ToString(),
            before is null ? AuditAction.Create : AuditAction.Update,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(saved),
            null,
            cancellationToken);

        return saved;
    }

    private static string? NormalizeNullable(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static IReadOnlyList<string> NormalizeMediosPago(IReadOnlyList<string>? mediosPago)
    {
        var source = mediosPago is { Count: > 0 } ? mediosPago : DefaultMediosPago;

        var normalized = source
            .Select(m => (m ?? string.Empty).Trim().ToUpperInvariant())
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (!normalized.Contains("EFECTIVO", StringComparer.OrdinalIgnoreCase))
        {
            normalized.Insert(0, "EFECTIVO");
        }

        return normalized;
    }

    private static string NormalizeMedioPagoHabitual(string? medioPagoHabitual, IReadOnlyList<string> mediosPago)
    {
        var normalized = string.IsNullOrWhiteSpace(medioPagoHabitual)
            ? "EFECTIVO"
            : medioPagoHabitual.Trim().ToUpperInvariant();

        return mediosPago.Contains(normalized, StringComparer.OrdinalIgnoreCase)
            ? normalized
            : "EFECTIVO";
    }

    private Guid EnsureTenant()
    {
        if (_requestContext.TenantId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        return _requestContext.TenantId;
    }
}


