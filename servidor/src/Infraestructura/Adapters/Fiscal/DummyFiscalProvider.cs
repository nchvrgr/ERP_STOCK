using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Comprobantes;

namespace Servidor.Infraestructura.Adapters.Fiscal;

public sealed class DummyFiscalProvider : IFiscalProvider
{
    public Task<FiscalEmitResultDto> EmitirAsync(
        FiscalEmitRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // Punto de extension: reemplazar por proveedor real (AFIP) manteniendo la misma interfaz.
        var numero = $"DUMMY-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        var payload = JsonSerializer.Serialize(new
        {
            request.ComprobanteId,
            request.VentaId,
            request.Total,
            request.Fecha,
            numero
        });

        var result = new FiscalEmitResultDto(
            "DUMMY",
            numero,
            payload,
            DateTimeOffset.UtcNow);

        return Task.FromResult(result);
    }
}


