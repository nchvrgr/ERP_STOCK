namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record SolicitudLoginDto(string FirebaseEmail, Guid? TenantId, Guid? SucursalId);



