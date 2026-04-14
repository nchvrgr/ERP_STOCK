namespace Servidor.Aplicacion.Dtos.Ventas;

public sealed record VentaDto(
    Guid Id,
    long Numero,
    Guid SucursalId,
    Guid? UserId,
    string Estado,
    string ListaPrecio,
    decimal TotalNeto,
    decimal TotalPagos,
    DateTimeOffset CreatedAt,
    IReadOnlyCollection<VentaItemDto> Items,
    bool Facturada = false,
    string? TipoFactura = null,
    string? ClienteNombre = null,
    string? ClienteCuit = null,
    string? ClienteDireccion = null,
    string? ClienteTelefono = null);

public sealed record VentaTicketDto(
    VentaDto Venta,
    IReadOnlyCollection<VentaPagoDto> Pagos);


