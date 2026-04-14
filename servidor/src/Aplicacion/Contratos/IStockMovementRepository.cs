using Servidor.Aplicacion.Dtos.Stock;
using Servidor.Dominio.Enums;

namespace Servidor.Aplicacion.Contratos;

public interface IStockMovementRepository
{
    Task<StockMovimientoRegisterResult> RegisterAsync(
        Guid tenantId,
        Guid sucursalId,
        StockMovimientoTipo tipo,
        string motivo,
        IReadOnlyCollection<StockMovimientoItemCreateDto> items,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockMovimientoDto>> SearchAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid? productoId,
        long? ventaNumero,
        bool? facturada,
        DateTimeOffset? desde,
        DateTimeOffset? hasta,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FacturaPendienteDto>> GetFacturasPendientesAsync(
        Guid tenantId,
        Guid sucursalId,
        CancellationToken cancellationToken = default);

    Task<bool> MarcarFacturaPendienteResueltaAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid movimientoId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<StockMovimientoRegisterResult> RevertAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid movimientoId,
        string motivo,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);
}


