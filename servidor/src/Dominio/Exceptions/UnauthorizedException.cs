namespace Servidor.Dominio.Exceptions;

public sealed class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

