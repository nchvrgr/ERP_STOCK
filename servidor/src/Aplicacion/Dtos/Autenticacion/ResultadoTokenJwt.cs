namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record ResultadoTokenJwt(string Token, DateTimeOffset ExpiresAt);



