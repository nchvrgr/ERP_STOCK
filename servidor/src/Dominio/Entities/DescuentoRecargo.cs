using Servidor.Dominio.Common;
using Servidor.Dominio.Enums;

namespace Servidor.Dominio.Entities;

public sealed class DescuentoRecargo : EntityBase
{
    private DescuentoRecargo()
    {
    }

    public DescuentoRecargo(
        Guid id,
        Guid tenantId,
        string name,
        decimal porcentaje,
        DescuentoRecargoTipo tipo,
        DateTimeOffset createdAtUtc)
        : base(id, tenantId, createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (porcentaje <= 0 || porcentaje > 100) throw new ArgumentException("Porcentaje must be > 0 and <= 100.", nameof(porcentaje));

        Name = name.Trim();
        Porcentaje = porcentaje;
        Tipo = tipo;
    }

    public string Name { get; private set; } = string.Empty;
    public decimal Porcentaje { get; private set; }
    public DescuentoRecargoTipo Tipo { get; private set; }

    public void Update(string name, decimal porcentaje, DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (porcentaje <= 0 || porcentaje > 100) throw new ArgumentException("Porcentaje must be > 0 and <= 100.", nameof(porcentaje));

        Name = name.Trim();
        Porcentaje = porcentaje;
        MarkUpdated(updatedAtUtc);
    }
}
