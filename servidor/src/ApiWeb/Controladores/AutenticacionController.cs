using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Autenticacion;
using Servidor.Aplicacion.CasosDeUso.Autenticacion;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/auth")]
public sealed class AutenticacionController : ControllerBase
{
    private readonly ServicioAutenticacion _servicioAutenticacion;

    public AutenticacionController(ServicioAutenticacion servicioAutenticacion)
    {
        _servicioAutenticacion = servicioAutenticacion;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(RespuestaLoginDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RespuestaLoginDto>> IniciarSesion(
        [FromBody] SolicitudLoginDto solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioAutenticacion.IniciarSesionAsync(solicitud, cancellationToken);
        return Ok(resultado);
    }
}





