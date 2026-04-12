using Microsoft.EntityFrameworkCore;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Usuarios;
using Servidor.Dominio.Entities;
using Servidor.Infraestructura.Persistence;

namespace Servidor.Infraestructura.Repositories;

public sealed class RepositorioUsuariosAdmin : IRepositorioUsuariosAdmin
{
    private readonly PosDbContext _dbContext;

    public RepositorioUsuariosAdmin(PosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<UsuarioAdminDto>> GetUsersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Usuarios
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        var roleLookup = await (from ur in _dbContext.UsuarioRoles.AsNoTracking()
            join r in _dbContext.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.TenantId == tenantId && r.TenantId == tenantId
            select new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleLookup
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<string>)g.Select(x => x.Name).Distinct().OrderBy(x => x).ToList());

        return users
            .Select(user => new UsuarioAdminDto(
                user.Id,
                user.Username,
                user.IsActive,
                rolesByUser.TryGetValue(user.Id, out var roles) ? roles : Array.Empty<string>()))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetAvailableRoleNamesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetRoleIdsByNamesAsync(Guid tenantId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
    {
        var names = roleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var roles = await _dbContext.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && names.Contains(r.Name))
            .ToListAsync(cancellationToken);

        return roles.ToDictionary(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public Task<bool> UserExistsAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Usuarios.AsNoTracking()
            .AnyAsync(u => u.TenantId == tenantId && u.Id == userId, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(Guid tenantId, string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Usuarios.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.Username == username);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task CreateUserAsync(User user, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        _dbContext.Usuarios.Add(user);

        var now = DateTimeOffset.UtcNow;
        foreach (var roleId in roleIds.Distinct())
        {
            _dbContext.UsuarioRoles.Add(new UserRole(Guid.NewGuid(), user.TenantId, user.Id, roleId, now));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetUserByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Usuarios
            .Where(u => u.TenantId == tenantId && u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ReplaceUserRolesAsync(Guid tenantId, Guid userId, IReadOnlyCollection<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.UsuarioRoles
            .Where(ur => ur.TenantId == tenantId && ur.UserId == userId)
            .ToListAsync(cancellationToken);

        var desiredRoleIds = roleIds.Distinct().ToHashSet();
        var toRemove = existing.Where(ur => !desiredRoleIds.Contains(ur.RoleId)).ToList();
        if (toRemove.Count > 0)
        {
            _dbContext.UsuarioRoles.RemoveRange(toRemove);
        }

        var existingRoleIds = existing.Select(ur => ur.RoleId).ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var roleId in desiredRoleIds.Where(roleId => !existingRoleIds.Contains(roleId)))
        {
            _dbContext.UsuarioRoles.Add(new UserRole(Guid.NewGuid(), tenantId, userId, roleId, now));
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
