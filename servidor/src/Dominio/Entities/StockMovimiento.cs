using Servidor.Dominio.Common;
using Servidor.Dominio.Enums;

namespace Servidor.Dominio.Entities;

public sealed class StockMovimiento : EntityBase
{
    private StockMovimiento()
    {
    }

    public StockMovimiento(
        Guid id,
        Guid tenantId,
        Guid sucursalId,
        StockMovimientoTipo tipo,
        string motivo,
        DateTimeOffset fecha,
        DateTimeOffset createdAtUtc,
        long? ventaNumero = null,
        bool? ventaFacturada = null,
        string? ventaTipoFactura = null,
        string? ventaClienteNombre = null,
        string? ventaClienteCuit = null,
        string? ventaClienteDireccion = null,
        string? ventaClienteTelefono = null,
        decimal? ventaTotalNeto = null,
        bool facturaPendiente = false)
        : base(id, tenantId, createdAtUtc)
    {
        if (sucursalId == Guid.Empty) throw new ArgumentException("SucursalId is required.", nameof(sucursalId));
        if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Motivo is required.", nameof(motivo));

        SucursalId = sucursalId;
        Tipo = tipo;
        Motivo = motivo;
        Fecha = fecha;
        VentaNumero = ventaNumero;
        VentaFacturada = ventaFacturada;
        VentaTipoFactura = string.IsNullOrWhiteSpace(ventaTipoFactura) ? null : ventaTipoFactura.Trim().ToUpperInvariant();
        VentaClienteNombre = string.IsNullOrWhiteSpace(ventaClienteNombre) ? null : ventaClienteNombre.Trim();
        VentaClienteCuit = string.IsNullOrWhiteSpace(ventaClienteCuit) ? null : ventaClienteCuit.Trim();
        VentaClienteDireccion = string.IsNullOrWhiteSpace(ventaClienteDireccion) ? null : ventaClienteDireccion.Trim();
        VentaClienteTelefono = string.IsNullOrWhiteSpace(ventaClienteTelefono) ? null : ventaClienteTelefono.Trim();
        VentaTotalNeto = ventaTotalNeto;
        FacturaPendiente = facturaPendiente;
    }

    public Guid SucursalId { get; private set; }
    public StockMovimientoTipo Tipo { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public DateTimeOffset Fecha { get; private set; }
    public long? VentaNumero { get; private set; }
    public bool? VentaFacturada { get; private set; }
    public string? VentaTipoFactura { get; private set; }
    public string? VentaClienteNombre { get; private set; }
    public string? VentaClienteCuit { get; private set; }
    public string? VentaClienteDireccion { get; private set; }
    public string? VentaClienteTelefono { get; private set; }
    public decimal? VentaTotalNeto { get; private set; }
    public bool FacturaPendiente { get; private set; }

    public void MarcarFacturaPendienteResuelta(DateTimeOffset updatedAtUtc)
    {
        FacturaPendiente = false;
        MarkUpdated(updatedAtUtc);
    }
}

