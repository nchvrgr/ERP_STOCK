namespace Servidor.Aplicacion.Contratos;

public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string passwordHash);
}


