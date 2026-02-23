namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record RespuestaLoginDto(string Token, DateTimeOffset ExpiresAt);



