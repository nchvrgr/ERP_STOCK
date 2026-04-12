namespace Servidor.Aplicacion.Dtos.Usuarios;

public sealed record UsuarioAdminDto(
    Guid Id,
    string Username,
    bool IsActive,
    IReadOnlyCollection<string> Roles);
