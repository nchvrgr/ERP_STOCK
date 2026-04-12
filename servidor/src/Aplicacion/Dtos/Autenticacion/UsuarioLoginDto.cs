namespace Servidor.Aplicacion.Dtos.Autenticacion;

public sealed record UsuarioLoginDto(
    Guid TenantId,
    Guid SucursalId,
    Guid UserId,
    string Username,
    string PasswordHash,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    bool IsActive);



