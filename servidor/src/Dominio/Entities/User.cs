using Servidor.Dominio.Common;

namespace Servidor.Dominio.Entities;

public sealed class User : EntityBase
{
    private User()
    {
    }

    public User(Guid id, Guid tenantId, string username, string passwordHash, DateTimeOffset createdAtUtc, bool isActive = true)
        : base(id, tenantId, createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));

        Username = username;
        PasswordHash = passwordHash;
        IsActive = isActive;
    }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void UpdateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
        Username = username.Trim();
    }

    public void UpdatePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}

