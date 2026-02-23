using Servidor.Aplicacion.Dtos.Etiquetas;
using Servidor.Aplicacion.Dtos.Productos;

namespace Servidor.Aplicacion.Contratos;

public interface IRepositorioProductos
{
    Task<IReadOnlyList<ProductoListaItemDto>> SearchAsync(
        Guid tenantId,
        string? search,
        Guid? categoriaId,
        bool? activo,
        CancellationToken cancellationToken = default);

    Task<ProductoDetalleDto?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        Guid tenantId,
        ProductoCrearDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        Guid tenantId,
        Guid productId,
        ProductoActualizarDto request,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<ProductoCodigoDto?> AddCodeAsync(
        Guid tenantId,
        Guid productId,
        string code,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<ProductoCodigoDto?> RemoveCodeAsync(
        Guid tenantId,
        Guid productId,
        Guid codeId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid tenantId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<ProductoProveedorDto?> AddProveedorAsync(
        Guid tenantId,
        Guid productId,
        Guid proveedorId,
        bool esPrincipal,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<ProductoProveedorDto?> SetProveedorPrincipalAsync(
        Guid tenantId,
        Guid productId,
        Guid relationId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetIdBySkuAsync(
        Guid tenantId,
        string sku,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetIdByCodeAsync(
        Guid tenantId,
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Dtos.Etiquetas.EtiquetaItemDto>> GetLabelDataAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        string listaPrecio,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CodigoBarraProductoDto>> GetBarcodeDataAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default);
}




