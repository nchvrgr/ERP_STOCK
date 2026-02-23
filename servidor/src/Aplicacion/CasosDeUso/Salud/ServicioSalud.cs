using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos;

namespace Servidor.Aplicacion.CasosDeUso.Salud;

public sealed class ServicioSalud : IServicioSalud
{
    public Task<EstadoSaludDto> ObtenerAsync(CancellationToken cancellationToken = default)
    {
        var dto = new EstadoSaludDto("ok", DateTimeOffset.UtcNow);
        return Task.FromResult(dto);
    }
}





