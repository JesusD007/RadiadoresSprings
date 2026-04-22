using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? RFC { get; set; }
    public TipoCliente Tipo { get; set; } = TipoCliente.Regular;
    public decimal LimiteCredito { get; set; } = 0;
    public decimal SaldoPendiente { get; set; } = 0;
    public bool EsActivo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<CuentaCobrar> CuentasCobrar { get; set; } = [];
    public ICollection<Orden> Ordenes { get; set; } = [];
    public ICollection<Pago> Pagos { get; set; } = [];

    // Métodos de dominio
    public decimal CreditoDisponible() => LimiteCredito - SaldoPendiente;
    public bool PuedeCreditarse(decimal monto) => CreditoDisponible() >= monto;
}
