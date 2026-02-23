using Servidor.Aplicacion.Dtos.DocumentosCompra;

namespace Servidor.Aplicacion.Contratos;

public interface IDocumentoCompraRepository
{
    Task<Guid> CreateAsync(
        Guid tenantId,
        Guid sucursalId,
        ParsedDocumentDto parsed,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task<DocumentoCompraDto?> GetByIdAsync(
        Guid tenantId,
        Guid sucursalId,
        Guid documentoCompraId,
        CancellationToken cancellationToken = default);
}


