using Microsoft.EntityFrameworkCore;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Empresa;
using Servidor.Dominio.Entities;
using Servidor.Infraestructura.Persistence;
using System.Text.Json;

namespace Servidor.Infraestructura.Repositories;

public sealed class EmpresaDatosRepository : IEmpresaDatosRepository
{
    private readonly PosDbContext _dbContext;

    public EmpresaDatosRepository(PosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmpresaDatosDto?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.EmpresaDatos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        return entity is null ? null : ToDto(entity);
    }

    public async Task<EmpresaDatosDto> UpsertAsync(
        Guid tenantId,
        EmpresaDatosUpsertDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.EmpresaDatos
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        if (entity is null)
        {
            entity = new EmpresaDatos(
                Guid.NewGuid(),
                tenantId,
                request.RazonSocial,
                request.Cuit,
                request.Telefono,
                request.Direccion,
                request.Email,
                request.Web,
                request.Observaciones,
                request.MedioPagoHabitual,
                SerializeMediosPago(request.MediosPago),
                nowUtc);

            _dbContext.EmpresaDatos.Add(entity);
        }
        else
        {
            entity.Update(
                request.RazonSocial,
                request.Cuit,
                request.Telefono,
                request.Direccion,
                request.Email,
                request.Web,
                request.Observaciones,
                request.MedioPagoHabitual,
                SerializeMediosPago(request.MediosPago),
                nowUtc);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(entity);
    }

    private static EmpresaDatosDto ToDto(EmpresaDatos entity)
    {
        var mediosPago = DeserializeMediosPago(entity.MediosPagoJson);
        return new EmpresaDatosDto(
            entity.Id,
            entity.RazonSocial,
            entity.Cuit,
            entity.Telefono,
            entity.Direccion,
            entity.Email,
            entity.Web,
            entity.Observaciones,
            entity.MedioPagoHabitual,
            mediosPago);
    }

    private static string? SerializeMediosPago(IReadOnlyList<string>? mediosPago)
    {
        if (mediosPago is null || mediosPago.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(mediosPago);
    }

    private static IReadOnlyList<string> DeserializeMediosPago(string? mediosPagoJson)
    {
        if (string.IsNullOrWhiteSpace(mediosPagoJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(mediosPagoJson);
            return parsed ?? new List<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}


