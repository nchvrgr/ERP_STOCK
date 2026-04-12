using Servidor.Aplicacion.Dtos.Usuarios;
using Servidor.Dominio.Entities;

namespace Servidor.Aplicacion.Contratos;

public interface IRepositorioUsuariosAdmin
{
    Task<IReadOnlyList<UsuarioAdminDto>> GetUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAvailableRoleNamesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, Guid>> GetRoleIdsByNamesAsync(Guid tenantId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(Guid tenantId, string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
    Task CreateUserAsync(User user, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task ReplaceUserRolesAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
