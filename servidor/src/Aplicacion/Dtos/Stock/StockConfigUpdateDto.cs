namespace Servidor.Aplicacion.Dtos.Stock;

public sealed record StockConfigUpdateDto(decimal? StockMinimo, decimal? StockDeseado, decimal? ToleranciaPct);


