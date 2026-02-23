using Servidor.Aplicacion.Dtos.Autenticacion;

namespace Servidor.Aplicacion.Contratos;

public interface IGeneradorTokenJwt
{
    ResultadoTokenJwt GenerateToken(SolicitudTokenJwt request);
}




