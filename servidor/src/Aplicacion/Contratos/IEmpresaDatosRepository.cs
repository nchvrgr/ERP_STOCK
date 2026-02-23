using Servidor.Aplicacion.Dtos.Empresa;

namespace Servidor.Aplicacion.Contratos;

public interface IEmpresaDatosRepository
{
    Task<EmpresaDatosDto?> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<EmpresaDatosDto> UpsertAsync(
        Guid tenantId,
        EmpresaDatosUpsertDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}


