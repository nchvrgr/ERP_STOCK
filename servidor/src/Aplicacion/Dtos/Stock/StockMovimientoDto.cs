namespace Servidor.Aplicacion.Dtos.Stock;

public sealed record StockMovimientoDto(
    Guid Id,
    string Tipo,
    string Motivo,
    DateTimeOffset Fecha,
    long? VentaNumero,
    bool? VentaFacturada,
    string? VentaTipoFactura,
    string? VentaClienteNombre,
    string? VentaClienteCuit,
    string? VentaClienteDireccion,
    string? VentaClienteTelefono,
    decimal? VentaTotalNeto,
    bool FacturaPendiente,
    IReadOnlyCollection<StockMovimientoItemDto> Items);

public sealed record FacturaPendienteDto(
    Guid MovimientoId,
    DateTimeOffset Fecha,
    long? VentaNumero,
    string? TipoFactura,
    string? ClienteNombre,
    string? ClienteCuit,
    string? ClienteDireccion,
    string? ClienteTelefono,
    decimal? MontoTotal,
    string Motivo);

public sealed record StockMovimientoItemDto(
    Guid Id,
    Guid ProductoId,
    string Nombre,
    string Sku,
    decimal Cantidad,
    bool EsIngreso,
    decimal? SaldoAntes,
    decimal? SaldoDespues);

public sealed record StockSaldoChangeDto(
    Guid MovimientoId,
    Guid MovimientoItemId,
    Guid ProductoId,
    decimal SaldoAntes,
    decimal SaldoDespues);

public sealed record StockMovimientoRegisterResult(
    StockMovimientoDto Movimiento,
    IReadOnlyCollection<StockSaldoChangeDto> Cambios);


