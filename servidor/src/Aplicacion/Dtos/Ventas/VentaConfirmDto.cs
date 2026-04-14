using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Aplicacion.Dtos.Stock;

namespace Servidor.Aplicacion.Dtos.Ventas;

public sealed record VentaPagoRequestDto(string MedioPago, decimal Monto);

public sealed record VentaClienteFacturaDto(
    string Nombre,
    string? Cuit = null,
    string? Direccion = null,
    string? Telefono = null);

public sealed record VentaConfirmRequestDto(
    IReadOnlyCollection<VentaPagoRequestDto> Pagos,
    Guid? CajaSesionId = null,
    bool? Facturada = null,
    string? TipoFactura = null,
    VentaClienteFacturaDto? Cliente = null);

public sealed record VentaPagoDto(Guid Id, string MedioPago, decimal Monto);

public sealed record VentaConfirmResultDto(
    VentaDto Venta,
    IReadOnlyCollection<VentaPagoDto> Pagos,
    IReadOnlyCollection<StockSaldoChangeDto> StockCambios,
    IReadOnlyCollection<CajaMovimientoResultDto> CajaMovimientos);


