using Servidor.Aplicacion.Dtos;

namespace Servidor.Aplicacion.Contratos;

public interface IServicioSalud
{
    Task<EstadoSaludDto> ObtenerAsync(CancellationToken cancellationToken = default);
}





