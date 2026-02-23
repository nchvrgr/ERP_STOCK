namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record SolicitudTokenJwt(
    Guid TenantId,
    Guid SucursalId,
    Guid UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);



