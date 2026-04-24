namespace IntegrationApp.Contracts.Responses.Pagos;

public record SimularPagoResponse
{
    public int TransaccionId { get; init; }
    public string Resultado { get; init; } = string.Empty;   // "Aprobado" | "Rechazado" | "PendienteSync"
    public string? Estado { get; init; }                     // Campo alternativo usado en modo offline
    public decimal Monto { get; init; }
    public string? MetodoPago { get; init; }
    public string? CodigoAuth { get; init; }
    public string? MensajeError { get; init; }
    public string? Mensaje { get; init; }
    public bool Offline { get; init; }
}
