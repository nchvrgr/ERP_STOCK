namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record SolicitudLoginDto(string FirebaseEmail, string? ErpUsername, Guid? TenantId, Guid? SucursalId);



