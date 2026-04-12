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
    private readonly ServicioUsuariosAdmin _servicioUsuariosAdmin;

    public UsuariosController(ServicioRolesUsuario servicioRolesUsuario, ServicioUsuariosAdmin servicioUsuariosAdmin)
    {
        _servicioRolesUsuario = servicioRolesUsuario;
        _servicioUsuariosAdmin = servicioUsuariosAdmin;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    [ProducesResponseType(typeof(RespuestaUsuariosAdminDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<RespuestaUsuariosAdminDto>> Listar(CancellationToken cancellationToken)
    {
        var response = await _servicioUsuariosAdmin.ListarAsync(cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UsuarioAdminDto>> Crear([FromBody] SolicitudCrearUsuarioDto solicitud, CancellationToken cancellationToken)
    {
        var created = await _servicioUsuariosAdmin.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(Listar), new { userId = created.Id }, created);
    }

    [HttpPut("{userId:guid}")]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    [ProducesResponseType(typeof(UsuarioAdminDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UsuarioAdminDto>> Actualizar(Guid userId, [FromBody] SolicitudActualizarUsuarioDto solicitud, CancellationToken cancellationToken)
    {
        var updated = await _servicioUsuariosAdmin.ActualizarAsync(userId, solicitud, cancellationToken);
        return Ok(updated);
    }

    [HttpPost("admin/password")]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    public async Task<IActionResult> CambiarContrasenaAdmin([FromBody] SolicitudCambiarContrasenaAdminDto solicitud, CancellationToken cancellationToken)
    {
        await _servicioUsuariosAdmin.CambiarContrasenaAdminAsync(solicitud, cancellationToken);
        return NoContent();
    }

    [HttpPut("{userId:guid}/roles")]
    [Authorize(Policy = "PERM_USUARIO_ADMIN")]
    public async Task<IActionResult> ActualizarRoles(Guid userId, [FromBody] SolicitudActualizarRolesUsuarioDto solicitud, CancellationToken cancellationToken)
    {
        await _servicioRolesUsuario.ActualizarRolesAsync(userId, solicitud, cancellationToken);
        return NoContent();
    }
}





