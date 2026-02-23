namespace Servidor.Aplicacion.Dtos.Productos;

public sealed record ProductoProveedorDto(
    Guid Id,
    Guid ProveedorId,
    string Proveedor,
    bool EsPrincipal);

public sealed record ProductoProveedorCrearDto(
    Guid ProveedorId,
    bool? EsPrincipal);

public sealed record ProductoProveedorActualizarDto(
    bool EsPrincipal);



