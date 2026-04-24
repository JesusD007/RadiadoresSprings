namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Venta persistida localmente cuando el Core no está disponible.
/// Esperará en cola hasta que el Circuit Breaker cierre.
/// </summary>
public class VentaOfflinePendiente
{
    public long Id { get; set; }
    public Guid IdTransaccionLocal { get; set; }
    public int CajeroId { get; set; }
    public int SucursalId { get; set; }
    public int ClienteId { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public decimal MontoRecibido { get; set; }
    public string LineasJson { get; set; } = string.Empty;  // JSON serializado de las líneas
    public DateTimeOffset FechaLocal { get; set; }
    public string Estado { get; set; } = "Pendiente";       // "Pendiente" | "EnCola" | "Sincronizada" | "Rechazada"
    public int IntentosSync { get; set; } = 0;
    public DateTimeOffset? UltimoIntento { get; set; }
}
