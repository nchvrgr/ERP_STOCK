using Servidor.Aplicacion.Dtos.Recepciones;

namespace Servidor.Aplicacion.Contratos;

public interface IRecepcionRepository
{
    Task<RecepcionConfirmResultDto> ConfirmarAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid preRecepcionId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}


