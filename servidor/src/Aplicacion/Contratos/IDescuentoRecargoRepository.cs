using Servidor.Aplicacion.Dtos.DescuentosRecargos;
using Servidor.Dominio.Enums;

namespace Servidor.Aplicacion.Contratos;

public interface IDescuentoRecargoRepository
{
    Task<IReadOnlyList<DescuentoRecargoDto>> SearchAsync(
        Guid tenantId,
        DescuentoRecargoTipo? tipo,
        string? search,
        CancellationToken cancellationToken = default);

    Task<DescuentoRecargoDto?> GetByIdAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        Guid tenantId,
        DescuentoRecargoCreateDto request,
        DescuentoRecargoTipo tipo,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        DescuentoRecargoUpdateDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid tenantId,
        Guid descuentoRecargoId,
        CancellationToken cancellationToken = default);
}
