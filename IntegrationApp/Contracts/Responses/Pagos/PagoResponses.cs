namespace IntegrationApp.Contracts.Responses.Pagos;

public record SimularPagoResponse
{
    public Guid TransaccionId { get; init; }
    public string Resultado { get; init; } = string.Empty;   // "Aprobado" | "Rechazado"
    public string? CodigoAuth { get; init; }
    public string? MensajeError { get; init; }
}
