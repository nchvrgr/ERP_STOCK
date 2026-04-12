namespace Servidor.Aplicacion.Dtos.Usuarios;

public sealed record SolicitudCambiarContrasenaAdminDto(
    string CurrentPassword,
    string NewPassword);
