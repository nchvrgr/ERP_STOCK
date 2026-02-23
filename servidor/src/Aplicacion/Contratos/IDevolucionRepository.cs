using Servidor.Aplicacion.Dtos.Devoluciones;

namespace Servidor.Aplicacion.Contratos;

public interface IDevolucionRepository
{
    Task<DevolucionResultDto> CreateAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid userId,
        DevolucionCreateDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}


