using Servidor.Aplicacion.Dtos.Auditoria;

namespace Servidor.Aplicacion.Contratos;

public interface IAuditLogQueryRepository
{
    Task<AuditLogQueryResultDto> SearchAsync(
        Guid tenantId,
        Guid sucursalId,
        AuditLogQueryRequestDto request,
        CancellationToken cancellationToken = default);
}


