namespace Servidor.Aplicacion.Dtos.Caja;

public sealed record CajaMovimientoCreateDto(string Tipo, decimal Monto, string Motivo, string? MedioPago);


