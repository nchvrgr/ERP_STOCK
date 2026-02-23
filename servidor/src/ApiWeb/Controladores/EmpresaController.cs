using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Empresa;
using Servidor.Aplicacion.CasosDeUso.Empresa;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/empresa")]
public sealed class EmpresaController : ControllerBase
{
    private readonly EmpresaDatosService _empresaDatosService;

    public EmpresaController(EmpresaDatosService empresaDatosService)
    {
        _empresaDatosService = empresaDatosService;
    }

    [HttpGet("datos")]
    [Authorize(Policy = "PERM_PRODUCTO_VER")]
    [ProducesResponseType(typeof(EmpresaDatosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmpresaDatosDto>> Get(CancellationToken cancellationToken)
    {
        var result = await _empresaDatosService.GetAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("datos")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(EmpresaDatosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmpresaDatosDto>> Upsert(
        [FromBody] EmpresaDatosUpsertDto request,
        CancellationToken cancellationToken)
    {
        var result = await _empresaDatosService.UpsertAsync(request, cancellationToken);
        return Ok(result);
    }
}


