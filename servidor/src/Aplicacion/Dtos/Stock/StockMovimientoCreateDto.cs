namespace Servidor.Aplicacion.Dtos.Stock;

public sealed record StockMovimientoCreateDto(
    string Tipo,
    string Motivo,
    IReadOnlyCollection<StockMovimientoItemCreateDto> Items);

public sealed record StockMovimientoItemCreateDto(
    Guid ProductoId,
    decimal Cantidad,
    bool EsIngreso);


