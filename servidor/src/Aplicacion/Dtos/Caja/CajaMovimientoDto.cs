namespace Servidor.Aplicacion.Dtos.Caja;

public sealed record CajaMovimientoDto(
    Guid Id,
    Guid CajaSesionId,
    string Tipo,
    string MedioPago,
    decimal Monto,
    string Motivo,
    DateTimeOffset Fecha);

public sealed record CajaMovimientoResultDto(
    CajaMovimientoDto Movimiento,
    decimal SaldoAntes,
    decimal SaldoDespues);


