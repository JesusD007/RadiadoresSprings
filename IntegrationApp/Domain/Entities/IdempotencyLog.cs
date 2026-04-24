namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Registra el IdTransaccionLocal de cada VentaRealizadaOfflineMessage
/// ya enviado al Core, para evitar reenvíos duplicados.
/// </summary>
public class IdempotencyLog
{
    public long Id { get; set; }
    public Guid IdTransaccionLocal { get; set; }
    public int? FacturaIdCore { get; set; }
    public string Estado { get; set; } = string.Empty;  // "Enviada" | "Aplicada" | "Rechazada"
    public string? MotivoRechazo { get; set; }
    public DateTimeOffset FechaEnvio { get; set; }
    public DateTimeOffset? FechaConfirmacion { get; set; }
}
