namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local de Cuentas por Cobrar.
/// Muestra el estado actual de las deudas para permitir cobros offline.
/// </summary>
public class CuentaCobrarMirror
{
    public int CoreId { get; set; }
    public int VentaId { get; set; }
    public int ClienteId { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal SaldoPendiente { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public string Estado { get; set; } = string.Empty; // "Pendiente", "Pagada", "Vencida", "Cancelada"
    public DateTime UltimaSync { get; set; }
}
