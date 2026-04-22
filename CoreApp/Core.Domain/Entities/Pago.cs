using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Pago
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int? VentaId { get; set; }
    public int? CuentaCobrarId { get; set; }
    public decimal Monto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? Referencia { get; set; }
    public string? Notas { get; set; }
    public int UsuarioId { get; set; }

    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public Venta? Venta { get; set; }
    public CuentaCobrar? CuentaCobrar { get; set; }
}
