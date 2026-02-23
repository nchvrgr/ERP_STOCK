using Servidor.Aplicacion.Dtos.Comprobantes;

namespace Servidor.Aplicacion.Contratos;

public interface IFiscalProvider
{
    Task<FiscalEmitResultDto> EmitirAsync(
        FiscalEmitRequestDto request,
        CancellationToken cancellationToken = default);
}


