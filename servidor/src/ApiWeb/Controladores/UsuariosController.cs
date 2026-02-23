using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Usuarios;
using Servidor.Aplicacion.CasosDeUso.Usuarios;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/users")]
public sealed class UsuariosController : ControllerBase
{
    private readonly ServicioRolesUsuario _servicioRolesUsuario;

    public UsuariosController(ServicioRolesUsuario servicioRolesUsuario)
    {
        _servicioRolesUsuario = servicioRolesUsuario;
    }

    [HttpPut("{userId:guid}/roles")]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    public async Task<IActionResult> ActualizarRoles(Guid userId, [FromBody] SolicitudActualizarRolesUsuarioDto solicitud, CancellationToken cancellationToken)
    {
        await _servicioRolesUsuario.ActualizarRolesAsync(userId, solicitud, cancellationToken);
        return NoContent();
    }
}





