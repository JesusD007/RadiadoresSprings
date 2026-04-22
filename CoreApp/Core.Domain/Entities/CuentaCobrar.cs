using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class CuentaCobrar
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int? VentaId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public decimal MontoOriginal { get; set; }
    public decimal MontoPagado { get; set; } = 0;
    public decimal SaldoPendiente => MontoOriginal - MontoPagado;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public DateTime FechaVencimiento { get; set; }
    public EstadoCuentaCobrar Estado { get; set; } = EstadoCuentaCobrar.Pendiente;
    public string? Notas { get; set; }

    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public Venta? Venta { get; set; }
    public ICollection<Pago> Pagos { get; set; } = [];

    public void AplicarPago(decimal monto)
    {
        MontoPagado += monto;
        Estado = MontoPagado >= MontoOriginal
            ? EstadoCuentaCobrar.Pagada
            : EstadoCuentaCobrar.PagoParcial;
    }

    public bool EstaVencida() => DateTime.UtcNow > FechaVencimiento && Estado != EstadoCuentaCobrar.Pagada;
}
