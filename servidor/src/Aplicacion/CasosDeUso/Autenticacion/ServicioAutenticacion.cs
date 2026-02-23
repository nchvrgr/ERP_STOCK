using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Autenticacion;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Autenticacion;

public sealed class ServicioAutenticacion
{
    private readonly IRepositorioAutenticacion _repositorioAutenticacion;
    private readonly IPasswordHasher _hasheadorContrasena;
    private readonly IGeneradorTokenJwt _generadorToken;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;

    public ServicioAutenticacion(
        IRepositorioAutenticacion repositorioAutenticacion,
        IPasswordHasher passwordHasher,
        IGeneradorTokenJwt tokenGenerator,
        IRequestContext requestContext,
        IAuditLogService auditLogService)
    {
        _repositorioAutenticacion = repositorioAutenticacion;
        _hasheadorContrasena = passwordHasher;
        _generadorToken = tokenGenerator;
        _contextoSolicitud = requestContext;
        _servicioAuditoria = auditLogService;
    }

    public async Task<RespuestaLoginDto> IniciarSesionAsync(SolicitudLoginDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["username"] = new[] { "El usuario es obligatorio." }
                });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["password"] = new[] { "La contrasena es obligatoria." }
                });
        }

        var usuarioLogin = await _repositorioAutenticacion.GetLoginUserAsync(
            request.Username.Trim(),
            request.TenantId,
            request.SucursalId,
            cancellationToken);

        if (usuarioLogin is null || !usuarioLogin.IsActive)
        {
            throw new UnauthorizedException("Credenciales invalidas.");
        }

        if (!_hasheadorContrasena.Verify(request.Password, usuarioLogin.PasswordHash))
        {
            throw new UnauthorizedException("Credenciales invalidas.");
        }

        var resultadoToken = _generadorToken.GenerateToken(new SolicitudTokenJwt(
            usuarioLogin.TenantId,
            usuarioLogin.SucursalId,
            usuarioLogin.UserId,
            usuarioLogin.Roles,
            usuarioLogin.Permissions));

        _contextoSolicitud.Set(usuarioLogin.TenantId, usuarioLogin.SucursalId, usuarioLogin.UserId);
        await _servicioAuditoria.LogAsync(
            "User",
            usuarioLogin.UserId.ToString(),
            Servidor.Dominio.Enums.AuditAction.Login,
            null,
            System.Text.Json.JsonSerializer.Serialize(new { roles = usuarioLogin.Roles, permissions = usuarioLogin.Permissions }),
            null,
            cancellationToken);

        return new RespuestaLoginDto(resultadoToken.Token, resultadoToken.ExpiresAt);
    }
}





