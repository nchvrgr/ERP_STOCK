using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Compras;
using Servidor.Aplicacion.CasosDeUso.Compras;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/ordenes-compra")]
public sealed class OrdenesCompraController : ControllerBase
{
    private readonly OrdenCompraService _servicioOrdenCompra;

    public OrdenesCompraController(OrdenCompraService servicioOrdenCompra)
    {
        _servicioOrdenCompra = servicioOrdenCompra;
    }

    [HttpPost]
    [Authorize(Policy = "PERM_COMPRAS_REGISTRAR")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrdenCompraDto>> Crear(
        [FromBody] OrdenCompraCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creada = await _servicioOrdenCompra.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creada.Id }, creada);
    }

    [HttpGet]
    [Authorize(Policy = "PERM_COMPRAS_REGISTRAR")]
    [ProducesResponseType(typeof(IReadOnlyList<OrdenCompraListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrdenCompraListItemDto>>> ObtenerLista(CancellationToken cancellationToken)
    {
        var resultado = await _servicioOrdenCompra.ObtenerListaAsync(cancellationToken);
        return Ok(resultado);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "PERM_COMPRAS_REGISTRAR")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdenCompraDto>> ObtenerPorId(Guid id, CancellationToken cancellationToken)
    {
        var orden = await _servicioOrdenCompra.ObtenerPorIdAsync(id, cancellationToken);
        return Ok(orden);
    }

    [HttpPost("{id:guid}/enviar")]
    [Authorize(Policy = "PERM_COMPRAS_REGISTRAR")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdenCompraDto>> Enviar(Guid id, CancellationToken cancellationToken)
    {
        var orden = await _servicioOrdenCompra.EnviarAsync(id, cancellationToken);
        return Ok(orden);
    }

    [HttpPost("{id:guid}/cancelar")]
    [Authorize(Policy = "PERM_COMPRAS_REGISTRAR")]
    [ProducesResponseType(typeof(OrdenCompraDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<OrdenCompraDto>> Cancelar(Guid id, CancellationToken cancellationToken)
    {
        var orden = await _servicioOrdenCompra.CancelarAsync(id, cancellationToken);
        return Ok(orden);
    }
}


