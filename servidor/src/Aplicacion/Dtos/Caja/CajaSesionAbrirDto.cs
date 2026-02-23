namespace Servidor.Aplicacion.Dtos.Caja;

public sealed record CajaSesionAbrirDto(Guid CajaId, decimal MontoInicial, string Turno);


