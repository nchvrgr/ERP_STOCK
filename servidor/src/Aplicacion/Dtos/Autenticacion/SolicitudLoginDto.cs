namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record SolicitudLoginDto(string Username, string Password, Guid? TenantId, Guid? SucursalId);



