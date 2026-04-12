namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record SolicitudLoginDto(
    string FirebaseEmail,
    bool EnterAsAdmin,
    string? ErpPassword,
    Guid? TenantId,
    Guid? SucursalId);



