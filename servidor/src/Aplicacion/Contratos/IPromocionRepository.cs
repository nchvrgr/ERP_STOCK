using Servidor.Aplicacion.Dtos.Precios;

namespace Servidor.Aplicacion.Contratos;

public interface IPromocionRepository
{
    Task<IReadOnlyList<PromocionAplicableDto>> GetActivasAsync(
        Guid tenantId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}


