namespace Servidor.Aplicacion.Dtos.Ventas;

public sealed record VentaScanRequestDto(string Code);

public sealed record VentaItemByProductRequestDto(Guid ProductId);

public sealed record VentaItemUpdateDto(decimal Cantidad);

public sealed record VentaItemChangeDto(
    VentaItemDto Item,
    decimal CantidadAntes,
    decimal CantidadDespues,
    bool Creado);


