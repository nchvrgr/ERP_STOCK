using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.ListasPrecio;
using Servidor.Aplicacion.CasosDeUso.ListasPrecio;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/listas-precio")]
public sealed class ListasPrecioController : ControllerBase
{
    private readonly ListaPrecioService _servicioListaPrecio;

    public ListasPrecioController(ListaPrecioService servicioListaPrecio)
    {
        _servicioListaPrecio = servicioListaPrecio;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(IReadOnlyList<ListaPrecioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ListaPrecioDto>>> ObtenerLista(CancellationToken cancellationToken)
    {
        var resultado = await _servicioListaPrecio.ObtenerListaAsync(cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ListaPrecioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ListaPrecioDto>> Crear(
        [FromBody] ListaPrecioCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creada = await _servicioListaPrecio.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(ObtenerLista), new { }, creada);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(ListaPrecioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ListaPrecioDto>> Actualizar(
        Guid id,
        [FromBody] ListaPrecioUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var actualizada = await _servicioListaPrecio.ActualizarAsync(id, solicitud, cancellationToken);
        return Ok(actualizada);
    }

    [HttpPut("{id:guid}/items")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpsertItems(
        Guid id,
        [FromBody] ListaPrecioItemsUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        await _servicioListaPrecio.ActualizarItemsAsync(id, solicitud, cancellationToken);
        return NoContent();
    }
}


