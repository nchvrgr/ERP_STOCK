using Servidor.Aplicacion.Dtos.Autenticacion;

namespace Servidor.Aplicacion.Contratos;

public interface IRepositorioAutenticacion
{
    Task<UsuarioLoginDto?> GetLoginUserAsync(
        string username,
        Guid? tenantId,
        Guid? sucursalId,
        CancellationToken cancellationToken = default);
}




