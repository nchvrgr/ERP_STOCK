using Servidor.Dominio.Common;

namespace Servidor.Dominio.Entities;

public sealed class EmpresaDatos : EntityBase
{
    private EmpresaDatos()
    {
    }

    public EmpresaDatos(
        Guid id,
        Guid tenantId,
        string razonSocial,
        string? cuit,
        string? telefono,
        string? direccion,
        string? email,
        string? web,
        string? observaciones,
        string? medioPagoHabitual,
        string? mediosPagoJson,
        DateTimeOffset createdAtUtc)
        : base(id, tenantId, createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentException("RazonSocial is required.", nameof(razonSocial));

        RazonSocial = razonSocial;
        Cuit = cuit;
        Telefono = telefono;
        Direccion = direccion;
        Email = email;
        Web = web;
        Observaciones = observaciones;
        MedioPagoHabitual = medioPagoHabitual;
        MediosPagoJson = mediosPagoJson;
    }

    public string RazonSocial { get; private set; } = string.Empty;
    public string? Cuit { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? Email { get; private set; }
    public string? Web { get; private set; }
    public string? Observaciones { get; private set; }
    public string? MedioPagoHabitual { get; private set; }
    public string? MediosPagoJson { get; private set; }

    public void Update(
        string razonSocial,
        string? cuit,
        string? telefono,
        string? direccion,
        string? email,
        string? web,
        string? observaciones,
        string? medioPagoHabitual,
        string? mediosPagoJson,
        DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(razonSocial)) throw new ArgumentException("RazonSocial is required.", nameof(razonSocial));

        RazonSocial = razonSocial;
        Cuit = cuit;
        Telefono = telefono;
        Direccion = direccion;
        Email = email;
        Web = web;
        Observaciones = observaciones;
        MedioPagoHabitual = medioPagoHabitual;
        MediosPagoJson = mediosPagoJson;
        MarkUpdated(updatedAtUtc);
    }
}

