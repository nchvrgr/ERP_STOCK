using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Productos;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Productos;

public sealed class ServicioProductos
{
    private readonly IRepositorioProductos _repositorioProductos;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public ServicioProductos(
        IRepositorioProductos repositorioProductos,
        IRequestContext requestContext,
        IAuditLogService auditLogService)
    {
        _repositorioProductos = repositorioProductos;
        _contextoSolicitud = requestContext;
        _servicioAuditoria = auditLogService;
    }

    public async Task<IReadOnlyList<ProductoListaItemDto>> BuscarAsync(
        string? search,
        Guid? categoriaId,
        bool? activo,
        bool? isCombo,
        CancellationToken cancellationToken)
    {
        var idTenant = AsegurarTenant();
        return await _repositorioProductos.SearchAsync(idTenant, search, categoriaId, activo, isCombo, cancellationToken);
    }

    public async Task<ProductoDetalleDto> ObtenerPorIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        var idTenant = AsegurarTenant();
        var producto = await _repositorioProductos.GetByIdAsync(idTenant, productId, cancellationToken);
        if (producto is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        return producto;
    }

    public async Task<ProductoDetalleDto> CrearAsync(ProductoCrearDto request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "El nombre es obligatorio." }
                });
        }

        if (string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "El SKU es obligatorio." }
                });
        }

        if (!SkuEsNumerico(request.Sku))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "El SKU debe contener solo numeros." }
                });
        }

        if (request.CategoriaId.HasValue && request.CategoriaId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["categoriaId"] = new[] { "La categoria es invalida." }
                });
        }

        if (request.MarcaId.HasValue && request.MarcaId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["marcaId"] = new[] { "La marca es invalida." }
                });
        }

        if (request.IsCombo == true)
        {
            if (request.ComboItems is null || request.ComboItems.Count == 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "El combo debe incluir productos." }
                    });
            }

            if (!request.PrecioBase.HasValue)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["precioBase"] = new[] { "El combo debe informar el total base." }
                    });
            }

            if (!request.PrecioVenta.HasValue)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["precioVenta"] = new[] { "El combo debe informar el precio final." }
                    });
            }
        }
        else if (!request.ProveedorId.HasValue || request.ProveedorId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        if (request.PrecioBase.HasValue && request.PrecioBase.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["precioBase"] = new[] { "El precio costo no puede ser negativo." }
                });
        }

        if (request.PrecioVenta.HasValue && request.PrecioVenta.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["precioVenta"] = new[] { "El precio de venta no puede ser negativo." }
                });
        }

        if (request.MargenGananciaPct.HasValue && request.MargenGananciaPct.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["margenGananciaPct"] = new[] { "El margen no puede ser negativo." }
                });
        }

        if (request.IsCombo == true)
        {
            await ValidarComboItemsAsync(request.ComboItems, null, cancellationToken);
        }

        var idTenant = AsegurarTenant();
        var now = DateTimeOffset.UtcNow;

        var normalizedRequest = request with
        {
            Name = request.Name.Trim(),
            Sku = request.Sku.Trim()
        };

        var newId = await _repositorioProductos.CreateAsync(idTenant, normalizedRequest, now, cancellationToken);
        var creado = await _repositorioProductos.GetByIdAsync(idTenant, newId, cancellationToken);
        if (creado is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Producto",
            creado.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(creado),
            null,
            cancellationToken);

        return creado;
    }

    public async Task<ProductoDetalleDto> ActualizarAsync(Guid productId, ProductoActualizarDto request, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        if (request is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        var hasAnyChange = request.Name is not null
            || request.Sku is not null
            || request.CategoriaId is not null
            || request.MarcaId is not null
            || request.ProveedorId is not null
            || request.IsActive is not null
            || request.PrecioBase is not null
            || request.PrecioVenta is not null
            || request.PricingMode is not null
            || request.MargenGananciaPct is not null
            || request.IsCombo.HasValue
            || request.ComboItems is not null;

        if (!hasAnyChange)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "Debe enviar al menos un cambio." }
                });
        }

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["name"] = new[] { "El nombre no puede estar vacio." }
                });
        }

        if (request.Sku is not null && string.IsNullOrWhiteSpace(request.Sku))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "El SKU no puede estar vacio." }
                });
        }

        if (request.Sku is not null && !SkuEsNumerico(request.Sku))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["sku"] = new[] { "El SKU debe contener solo numeros." }
                });
        }

        if (request.CategoriaId.HasValue && request.CategoriaId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["categoriaId"] = new[] { "La categoria es invalida." }
                });
        }

        if (request.MarcaId.HasValue && request.MarcaId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["marcaId"] = new[] { "La marca es invalida." }
                });
        }

        if (request.IsCombo == true)
        {
            if (request.ComboItems is null || request.ComboItems.Count == 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "El combo debe incluir productos." }
                    });
            }

            if (!request.PrecioBase.HasValue)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["precioBase"] = new[] { "El combo debe informar el total base." }
                    });
            }

            if (!request.PrecioVenta.HasValue)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["precioVenta"] = new[] { "El combo debe informar el precio final." }
                    });
            }
        }

        if (request.ProveedorId.HasValue && request.ProveedorId.Value == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es invalido." }
                });
        }

        if (request.PrecioBase.HasValue && request.PrecioBase.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["precioBase"] = new[] { "El precio costo no puede ser negativo." }
                });
        }

        if (request.PrecioVenta.HasValue && request.PrecioVenta.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["precioVenta"] = new[] { "El precio de venta no puede ser negativo." }
                });
        }

        if (request.MargenGananciaPct.HasValue && request.MargenGananciaPct.Value < 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["margenGananciaPct"] = new[] { "El margen no puede ser negativo." }
                });
        }

        if (request.IsCombo == true)
        {
            await ValidarComboItemsAsync(request.ComboItems, productId, cancellationToken);
        }

        var idTenant = AsegurarTenant();
        var previo = await _repositorioProductos.GetByIdAsync(idTenant, productId, cancellationToken);
        if (previo is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        var normalizedRequest = request with
        {
            Name = request.Name?.Trim(),
            Sku = request.Sku?.Trim()
        };

        var updated = await _repositorioProductos.UpdateAsync(idTenant, productId, normalizedRequest, DateTimeOffset.UtcNow, cancellationToken);
        if (!updated)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        var posterior = await _repositorioProductos.GetByIdAsync(idTenant, productId, cancellationToken);
        if (posterior is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Producto",
            posterior.Id.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(previo),
            JsonSerializer.Serialize(posterior),
            null,
            cancellationToken);

        return posterior;
    }

    public async Task<ProductoCodigoDto> AgregarCodigoAsync(Guid productId, ProductoCodigoCrearDto request, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["code"] = new[] { "El codigo es obligatorio." }
                });
        }

        var idTenant = AsegurarTenant();
        var code = request.Code.Trim();
        var resultado = await _repositorioProductos.AddCodeAsync(idTenant, productId, code, DateTimeOffset.UtcNow, cancellationToken);
        if (resultado is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "ProductoCodigo",
            resultado.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(resultado),
            JsonSerializer.Serialize(new { productoId = productId }),
            cancellationToken);

        return resultado;
    }

    public async Task QuitarCodigoAsync(Guid productId, Guid codeId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        if (codeId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["codeId"] = new[] { "El codigo es obligatorio." }
                });
        }

        var idTenant = AsegurarTenant();
        var removido = await _repositorioProductos.RemoveCodeAsync(idTenant, productId, codeId, cancellationToken);
        if (removido is null)
        {
            throw new NotFoundException("Codigo no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "ProductoCodigo",
            codeId.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(removido),
            null,
            JsonSerializer.Serialize(new { productoId = productId }),
            cancellationToken);
    }

    public async Task EliminarAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        var idTenant = AsegurarTenant();
        var previo = await _repositorioProductos.GetByIdAsync(idTenant, productId, cancellationToken);
        if (previo is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        var eliminado = await _repositorioProductos.DeleteAsync(idTenant, productId, cancellationToken);
        if (!eliminado)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "Producto",
            productId.ToString(),
            AuditAction.Delete,
            JsonSerializer.Serialize(previo),
            null,
            null,
            cancellationToken);
    }

    public async Task<ProductoProveedorDto> AgregarProveedorAsync(
        Guid productId,
        ProductoProveedorCrearDto request,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        if (request is null)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["request"] = new[] { "El request es obligatorio." }
                });
        }

        if (request.ProveedorId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["proveedorId"] = new[] { "El proveedor es obligatorio." }
                });
        }

        var idTenant = AsegurarTenant();
        var now = DateTimeOffset.UtcNow;
        var esPrincipal = request.EsPrincipal ?? false;

        var resultado = await _repositorioProductos.AddProveedorAsync(
            idTenant,
            productId,
            request.ProveedorId,
            esPrincipal,
            now,
            cancellationToken);

        if (resultado is null)
        {
            throw new NotFoundException("Producto no encontrado.");
        }

        await _servicioAuditoria.LogAsync(
            "ProductoProveedor",
            resultado.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(resultado),
            JsonSerializer.Serialize(new { productoId = productId }),
            cancellationToken);

        return resultado;
    }

    public async Task<ProductoProveedorDto> DefinirProveedorPrincipalAsync(
        Guid productId,
        Guid relationId,
        ProductoProveedorActualizarDto request,
        CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["productId"] = new[] { "El producto es obligatorio." }
                });
        }

        if (relationId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["relationId"] = new[] { "La relacion es obligatoria." }
                });
        }

        if (request is null || !request.EsPrincipal)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["esPrincipal"] = new[] { "Debe marcar como principal." }
                });
        }

        var idTenant = AsegurarTenant();
        var resultado = await _repositorioProductos.SetProveedorPrincipalAsync(
            idTenant,
            productId,
            relationId,
            DateTimeOffset.UtcNow,
            cancellationToken);

        if (resultado is null)
        {
            throw new NotFoundException("Proveedor no encontrado para el producto.");
        }

        await _servicioAuditoria.LogAsync(
            "ProductoProveedor",
            resultado.Id.ToString(),
            AuditAction.Update,
            null,
            JsonSerializer.Serialize(resultado),
            JsonSerializer.Serialize(new { productoId = productId }),
            cancellationToken);

        return resultado;
    }

    private Guid AsegurarTenant()
    {
        if (_contextoSolicitud.TenantId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        return _contextoSolicitud.TenantId;
    }

    private static bool SkuEsNumerico(string sku)
    {
        foreach (var ch in sku.Trim())
        {
            if (!char.IsDigit(ch))
            {
                return false;
            }
        }

        return !string.IsNullOrWhiteSpace(sku);
    }

    private async Task ValidarComboItemsAsync(
        IReadOnlyList<ProductoComboItemDto>? comboItems,
        Guid? currentProductId,
        CancellationToken cancellationToken)
    {
        if (comboItems is null || comboItems.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["comboItems"] = new[] { "El combo debe incluir productos." }
                });
        }

        var idTenant = AsegurarTenant();
        var seen = new HashSet<Guid>();

        foreach (var comboItem in comboItems)
        {
            if (comboItem.ProductoId == Guid.Empty || comboItem.Cantidad <= 0)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "Los productos del combo deben ser validos y con cantidad mayor a cero." }
                    });
            }

            if (!seen.Add(comboItem.ProductoId))
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "El combo no puede repetir productos." }
                    });
            }

            if (currentProductId.HasValue && comboItem.ProductoId == currentProductId.Value)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "El combo no puede incluirse a si mismo." }
                    });
            }

            var producto = await _repositorioProductos.GetByIdAsync(idTenant, comboItem.ProductoId, cancellationToken);
            if (producto is null)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "Uno de los productos del combo no existe." }
                    });
            }

            if (producto.IsCombo)
            {
                throw new ValidationException(
                    "Validacion fallida.",
                    new Dictionary<string, string[]>
                    {
                        ["comboItems"] = new[] { "Un combo no puede contener otro combo." }
                    });
            }
        }
    }
}








