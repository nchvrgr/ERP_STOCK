namespace Servidor.Aplicacion.Dtos.Usuarios;

public sealed record SolicitudActualizarUsuarioDto(
    string Username,
    string? Password,
    IReadOnlyCollection<string> Roles,
    bool IsActive);
