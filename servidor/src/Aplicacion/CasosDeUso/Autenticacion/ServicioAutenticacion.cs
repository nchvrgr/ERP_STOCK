using Servidor.Aplicacion.Contratos;
using Servidor.Aplicacion.Dtos.Autenticacion;
using Servidor.Dominio.Exceptions;

namespace Servidor.Aplicacion.CasosDeUso.Autenticacion;

public sealed class ServicioAutenticacion
{
    private const string AdminUsername = "admin";
    private const string CashierUsername = "cajero";

    private readonly IRepositorioAutenticacion _repositorioAutenticacion;
    private readonly IGeneradorTokenJwt _generadorToken;
    private readonly IRequestContext _contextoSolicitud;
    private readonly IAuditLogService _servicioAuditoria;
    private readonly IPasswordHasher _passwordHasher;

    public ServicioAutenticacion(
        IRepositorioAutenticacion repositorioAutenticacion,
        IGeneradorTokenJwt tokenGenerator,
        IRequestContext requestContext,
        IAuditLogService auditLogService,
        IPasswordHasher passwordHasher)
    {
        _repositorioAutenticacion = repositorioAutenticacion;
        _generadorToken = tokenGenerator;
        _contextoSolicitud = requestContext;
        _servicioAuditoria = auditLogService;
        _passwordHasher = passwordHasher;
    }

    public async Task<RespuestaLoginDto> IniciarSesionAsync(SolicitudLoginDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FirebaseEmail))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["firebaseEmail"] = new[] { "El usuario es obligatorio." }
                });
        }

        if (request.EnterAsAdmin && string.IsNullOrWhiteSpace(request.ErpPassword))
        {
            throw new ValidationException(
                "Validacion fallida.",
                new Dictionary<string, string[]>
                {
                    ["erpPassword"] = new[] { "La contraseña de administrador es obligatoria." }
                });
        }

        var resolvedUsername = request.EnterAsAdmin ? AdminUsername : CashierUsername;
        var usuarioLogin = await _repositorioAutenticacion.GetLoginUserAsync(
            request.FirebaseEmail.Trim(),
            resolvedUsername,
            request.TenantId,
            request.SucursalId,
            cancellationToken);

        if (usuarioLogin is null || !usuarioLogin.IsActive)
        {
            throw new UnauthorizedException("Credenciales invalidas.");
        }

        if (request.EnterAsAdmin && !_passwordHasher.Verify(request.ErpPassword!.Trim(), usuarioLogin.PasswordHash))
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
            System.Text.Json.JsonSerializer.Serialize(new { username = usuarioLogin.Username, roles = usuarioLogin.Roles, permissions = usuarioLogin.Permissions }),
            null,
            cancellationToken);

        return new RespuestaLoginDto(resultadoToken.Token, resultadoToken.ExpiresAt);
    }
}





