namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record UsuarioLoginDto(
    Guid TenantId,
    Guid SucursalId,
    Guid UserId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    bool IsActive);



