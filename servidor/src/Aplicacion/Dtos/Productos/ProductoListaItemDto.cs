namespace Servidor.Aplicacion.Dtos.Productos;

public sealed record ProductoListaItemDto(
    Guid Id,
    string Name,
    string Sku,
    string Codigo,
    Guid? CategoriaId,
    string? Categoria,
    Guid? MarcaId,
    string? Marca,
    Guid? ProveedorId,
    string? Proveedor,
    decimal PrecioBase,
    decimal PrecioVenta,
    string PricingMode,
    decimal? MargenGananciaPct,
    bool IsActive);



