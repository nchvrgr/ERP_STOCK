using Servidor.Aplicacion.Dtos.Etiquetas;

namespace Servidor.Aplicacion.Contratos;

public interface ICodigoBarraPdfGenerator
{
    byte[] Generate(CodigoBarraPdfDataDto data);
}


