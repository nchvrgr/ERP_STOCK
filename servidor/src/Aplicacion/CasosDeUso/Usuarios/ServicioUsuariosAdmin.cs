using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Usuarios;
using Servidor.Dominio.Entities;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Usuarios;

public sealed class ServicioUsuariosAdmin
{
    private const string AdminUsername = "admin";

    private readonly IRepositorioUsuariosAdmin _repositorioUsuariosAdmin;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _servicioAuditoria;

    public ServicioUsuariosAdmin(
        IRepositorioUsuariosAdmin repositorioUsuariosAdmin,
        IRequestContext requestContext,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService)
    {
        _repositorioUsuariosAdmin = repositorioUsuariosAdmin;
        _contextoSolicitud = requestContext;
        _passwordHasher = passwordHasher;
        _servicioAuditoria = auditLogService;
    }

    public async Task<RespuestaUsuariosAdminDto> ListarAsync(CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var items = await _repositorioUsuariosAdmin.GetUsersAsync(tenantId, cancellationToken);
        var roles = await _repositorioUsuariosAdmin.GetAvailableRoleNamesAsync(tenantId, cancellationToken);
        return new RespuestaUsuariosAdminDto(items, roles);
    }

    public async Task<UsuarioAdminDto> CrearAsync(SolicitudCrearUsuarioDto request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var normalizedUsername = NormalizeUsername(request.Username);
        var normalizedRoles = NormalizeRoles(request.Roles);
        ValidatePassword(request.Password);

        var availableRoleIds = await ResolveRoleIdsAsync(tenantId, normalizedRoles, cancellationToken);
        var exists = await _repositorioUsuariosAdmin.UsernameExistsAsync(tenantId, normalizedUsername, null, cancellationToken);
        if (exists)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["username"] = new[] { "Ya existe un usuario con ese nombre." }
                });
        }

        var user = new User(
            Guid.NewGuid(),
            tenantId,
            normalizedUsername,
            _passwordHasher.Hash(request.Password.Trim()),
            DateTimeOffset.UtcNow,
            request.IsActive);

        await _repositorioUsuariosAdmin.CreateUserAsync(user, availableRoleIds.Values.ToList(), cancellationToken);

        var created = new UsuarioAdminDto(user.Id, user.Username, user.IsActive, normalizedRoles);
        await _servicioAuditoria.LogAsync(
            "User",
            user.Id.ToString(),
            AuditAction.Create,
            null,
            JsonSerializer.Serialize(created),
            null,
            cancellationToken);

        return created;
    }

    public async Task<UsuarioAdminDto> ActualizarAsync(Guid userId, SolicitudActualizarUsuarioDto request, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["userId"] = new[] { "El usuario es obligatorio." }
                });
        }

        var tenantId = GetTenantId();
        var currentUserId = _contextoSolicitud.UserId;
        if (currentUserId == userId && !request.IsActive)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["isActive"] = new[] { "No puedes desactivar tu propia cuenta." }
                });
        }

        var user = await _repositorioUsuariosAdmin.GetUserByIdAsync(tenantId, userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuario no encontrado.");
        }

        var normalizedUsername = NormalizeUsername(request.Username);
        var normalizedRoles = NormalizeRoles(request.Roles);
        var availableRoleIds = await ResolveRoleIdsAsync(tenantId, normalizedRoles, cancellationToken);

        var exists = await _repositorioUsuariosAdmin.UsernameExistsAsync(tenantId, normalizedUsername, userId, cancellationToken);
        if (exists)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["username"] = new[] { "Ya existe un usuario con ese nombre." }
                });
        }

        var before = new UsuarioAdminDto(
            user.Id,
            user.Username,
            user.IsActive,
            (await _repositorioUsuariosAdmin.GetUsersAsync(tenantId, cancellationToken))
                .FirstOrDefault(x => x.Id == user.Id)?.Roles ?? Array.Empty<string>());

        user.UpdateUsername(normalizedUsername);
        user.SetActive(request.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            ValidatePassword(request.Password);
            user.UpdatePasswordHash(_passwordHasher.Hash(request.Password.Trim()));
        }

        await _repositorioUsuariosAdmin.ReplaceUserRolesAsync(tenantId, userId, availableRoleIds.Values.ToList(), cancellationToken);
        await _repositorioUsuariosAdmin.SaveChangesAsync(cancellationToken);

        var after = new UsuarioAdminDto(user.Id, user.Username, user.IsActive, normalizedRoles);
        await _servicioAuditoria.LogAsync(
            "User",
            user.Id.ToString(),
            AuditAction.Update,
            JsonSerializer.Serialize(before),
            JsonSerializer.Serialize(after),
            null,
            cancellationToken);

        return after;
    }

    public async Task CambiarContrasenaAdminAsync(SolicitudCambiarContrasenaAdminDto request, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var currentUserId = _contextoSolicitud.UserId;
        if (currentUserId == Guid.Empty)
        {
            throw new UnauthorizedException("Usuario no autenticado.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = new[] { "La contraseña actual es obligatoria." }
                });
        }

        ValidatePassword(request.NewPassword);

        var user = await _repositorioUsuariosAdmin.GetUserByIdAsync(tenantId, currentUserId, cancellationToken);
        if (user is null || !string.Equals(user.Username, AdminUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedException("Solo el administrador puede cambiar esta contraseña.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword.Trim(), user.PasswordHash))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["currentPassword"] = new[] { "La contraseña actual es incorrecta." }
                });
        }

        user.UpdatePasswordHash(_passwordHasher.Hash(request.NewPassword.Trim()));
        await _repositorioUsuariosAdmin.SaveChangesAsync(cancellationToken);

        await _servicioAuditoria.LogAsync(
            "User",
            user.Id.ToString(),
            AuditAction.Update,
            null,
            JsonSerializer.Serialize(new { passwordChanged = true }),
            null,
            cancellationToken);
    }

    private Guid GetTenantId()
    {
        var tenantId = _contextoSolicitud.TenantId;
        if (tenantId == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        return tenantId;
    }

    private static string NormalizeUsername(string username)
    {
        var normalized = username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["username"] = new[] { "El usuario es obligatorio." }
                });
        }

        return normalized;
    }

    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length < 4)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["password"] = new[] { "La contraseña debe tener al menos 4 caracteres." }
                });
        }
    }

    private static List<string> NormalizeRoles(IReadOnlyCollection<string>? roles)
    {
        var normalized = (roles ?? Array.Empty<string>())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { "Debe especificar al menos un rol." }
                });
        }

        return normalized;
    }

    private async Task<IReadOnlyDictionary<string, Guid>> ResolveRoleIdsAsync(Guid tenantId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken)
    {
        var roleIds = await _repositorioUsuariosAdmin.GetRoleIdsByNamesAsync(tenantId, roleNames, cancellationToken);
        var missing = roleNames.Where(role => !roleIds.ContainsKey(role)).ToList();
        if (missing.Count > 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { $"Roles invalidos: {string.Join(", ", missing)}" }
                });
        }

        return roleIds;
    }
}
