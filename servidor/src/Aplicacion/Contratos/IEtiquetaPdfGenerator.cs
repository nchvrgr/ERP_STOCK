using Servidor.Aplicacion.Dtos.Etiquetas;

namespace Servidor.Aplicacion.Contratos;

public interface IEtiquetaPdfGenerator
{
    byte[] Generate(EtiquetaPdfDataDto data);
}


