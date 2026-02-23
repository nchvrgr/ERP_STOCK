namespace Servidor.Aplicacion.Dtos.Productos;

public sealed record ProductoActualizarDto(
    string? Name,
    string? Sku,
    Guid? CategoriaId,
    Guid? MarcaId,
    Guid? ProveedorId,
    bool? IsActive,
    decimal? PrecioBase,
    decimal? PrecioVenta,
    string? PricingMode = null,
    decimal? MargenGananciaPct = null);



