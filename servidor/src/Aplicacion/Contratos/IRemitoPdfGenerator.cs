using Servidor.Aplicacion.Dtos.Stock;

namespace Servidor.Aplicacion.Contratos;

public interface IRemitoPdfGenerator
{
    byte[] Generate(StockRemitoPdfDataDto data);
}


