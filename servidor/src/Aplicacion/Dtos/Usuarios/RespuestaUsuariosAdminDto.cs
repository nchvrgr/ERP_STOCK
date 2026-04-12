namespace Servidor.Aplicacion.Dtos.Usuarios;

public sealed record RespuestaUsuariosAdminDto(
    IReadOnlyCollection<UsuarioAdminDto> Items,
    IReadOnlyCollection<string> AvailableRoles);
