using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Servidor.Aplicacion.Dtos.Categorias;
using Servidor.Aplicacion.CasosDeUso.Categorias;

namespace Servidor.ApiWeb.Controladores;

[ApiController]
[Route("api/v1/categorias-precio")]
public sealed class CategoriasPrecioController : ControllerBase
{
    private readonly CategoriaPrecioService _servicioCategoria;

    public CategoriasPrecioController(CategoriaPrecioService servicioCategoria)
    {
        _servicioCategoria = servicioCategoria;
    }

    [HttpGet]
    [Authorize(Policy = "PERM_PRODUCTO_VER")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaPrecioDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaPrecioDto>>> Buscar(
        [FromQuery] string? busqueda,
        [FromQuery] bool? activo,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicioCategoria.BuscarAsync(busqueda, activo, cancellationToken);
        return Ok(resultado);
    }

    [HttpPost]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(CategoriaPrecioDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoriaPrecioDto>> Crear(
        [FromBody] CategoriaPrecioCreateDto solicitud,
        CancellationToken cancellationToken)
    {
        var creada = await _servicioCategoria.CrearAsync(solicitud, cancellationToken);
        return CreatedAtAction(nameof(Buscar), new { }, creada);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Policy = "PERM_PRODUCTO_EDITAR")]
    [ProducesResponseType(typeof(CategoriaPrecioDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CategoriaPrecioDto>> Actualizar(
        Guid id,
        [FromBody] CategoriaPrecioUpdateDto solicitud,
        CancellationToken cancellationToken)
    {
        var actualizada = await _servicioCategoria.ActualizarAsync(id, solicitud, cancellationToken);
        return Ok(actualizada);
    }
}


