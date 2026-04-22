using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Venta
{
    public int Id { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public int SucursalId { get; set; }
    public int CajaId { get; set; }
    public int SesionCajaId { get; set; }
    public int? ClienteId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal IVA { get; set; }
    public decimal Total { get; set; }
    public decimal Descuento { get; set; } = 0;
    public MetodoPago MetodoPago { get; set; }
    public EstadoVenta Estado { get; set; } = EstadoVenta.Completada;
    public bool EsOffline { get; set; } = false;
    public string? IdTransaccionLocal { get; set; }
    public string? Observaciones { get; set; }

    // Navegación
    public Sucursal Sucursal { get; set; } = null!;
    public Caja Caja { get; set; } = null!;
    public SesionCaja SesionCaja { get; set; } = null!;
    public Cliente? Cliente { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public ICollection<LineaVenta> Lineas { get; set; } = [];
    public CuentaCobrar? CuentaCobrar { get; set; }

    // Métodos de dominio
    public void CalcularTotales(decimal tasaIVA = 0.16m)
    {
        Subtotal = Lineas.Sum(l => l.Subtotal);
        IVA = Subtotal * tasaIVA;
        Total = Subtotal + IVA - Descuento;
    }
}
