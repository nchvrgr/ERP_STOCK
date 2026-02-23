using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/health")]
public sealed class SaludController : ControllerBase
{
    private readonly IServicioSalud _servicioSalud;

    public SaludController(IServicioSalud servicioSalud)
    {
        _servicioSalud = servicioSalud;
    }

    [HttpGet]
    [ProducesResponseType(typeof(EstadoSaludDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EstadoSaludDto>> Obtener(CancellationToken cancellationToken)
    {
        var resultado = await _servicioSalud.ObtenerAsync(cancellationToken);
        return Ok(resultado);
    }
}






