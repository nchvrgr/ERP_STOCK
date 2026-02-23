using System.Text.Json;
using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Usuarios;
using Servidor.Dominio.Enums;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Usuarios;

public sealed class ServicioRolesUsuario
{
    private readonly IRepositorioRolesUsuario _repositorioRolesUsuario;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public ServicioRolesUsuario(
        IRepositorioRolesUsuario repositorioRolesUsuario,
        IRequestContext requestContext,
        IAuditLogService auditLogService)
    {
        _repositorioRolesUsuario = repositorioRolesUsuario;
        _contextoSolicitud = requestContext;
        _servicioAuditoria = auditLogService;
    }

    public async Task ActualizarRolesAsync(Guid userId, SolicitudActualizarRolesUsuarioDto request, CancellationToken cancellationToken)
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

        if (request is null || request.Roles is null || request.Roles.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { "Debe especificar al menos un rol." }
                });
        }

        var idTenant = _contextoSolicitud.TenantId;
        if (idTenant == Guid.Empty)
        {
            throw new UnauthorizedException("Contexto de tenant invalido.");
        }

        var rolesNormalizados = request.Roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rolesNormalizados.Count == 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { "Debe especificar al menos un rol valido." }
                });
        }

        var usuarioExiste = await _repositorioRolesUsuario.UserExistsAsync(idTenant, userId, cancellationToken);
        if (!usuarioExiste)
        {
            throw new NotFoundException("Usuario no encontrado.");
        }

        var mapaIdsRol = await _repositorioRolesUsuario.GetRoleIdsByNamesAsync(idTenant, rolesNormalizados, cancellationToken);
        var rolesFaltantes = rolesNormalizados.Where(role => !mapaIdsRol.ContainsKey(role)).ToList();
        if (rolesFaltantes.Count > 0)
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["roles"] = new[] { $"Roles invalidos: {string.Join(", ", rolesFaltantes)}" }
                });
        }

        var rolesPrevios = await _repositorioRolesUsuario.GetUserRoleNamesAsync(idTenant, userId, cancellationToken);

        await _repositorioRolesUsuario.ReplaceUserRolesAsync(idTenant, userId, mapaIdsRol.Values.ToList(), cancellationToken);

        var rolesPosteriores = rolesNormalizados.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();
        var jsonPrevio = JsonSerializer.Serialize(new { roles = rolesPrevios.OrderBy(r => r, StringComparer.OrdinalIgnoreCase) });
        var jsonPosterior = JsonSerializer.Serialize(new { roles = rolesPosteriores });

        await _servicioAuditoria.LogAsync(
            "UserRole",
            userId.ToString(),
            AuditAction.RoleChange,
            jsonPrevio,
            jsonPosterior,
            null,
            cancellationToken);
    }
}





