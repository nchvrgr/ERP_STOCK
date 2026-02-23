using Servidor.Aplicacion.Dtos.Caja;
using Servidor.Aplicacion.Dtos.Stock;

namespace Servidor.Aplicacion.Dtos.Ventas;

public sealed record VentaAnularRequestDto(string Motivo);

public sealed record VentaAnularResultDto(
    VentaDto Venta,
    IReadOnlyCollection<StockSaldoChangeDto> StockCambios,
    IReadOnlyCollection<CajaMovimientoResultDto> CajaMovimientos);


