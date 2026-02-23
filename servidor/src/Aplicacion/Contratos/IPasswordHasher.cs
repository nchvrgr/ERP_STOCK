namespace Servidor.Aplicacion.Contratos;

public interface IPasswordHasher
{
    bool Verify(string plainText, string passwordHash);
}


