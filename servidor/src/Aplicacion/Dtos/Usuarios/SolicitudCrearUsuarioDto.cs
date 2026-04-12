namespace Servidor.Aplicacion.Dtos.Usuarios;

public sealed record SolicitudCrearUsuarioDto(
    string Username,
    string Password,
    IReadOnlyCollection<string> Roles,
    bool IsActive = true);
