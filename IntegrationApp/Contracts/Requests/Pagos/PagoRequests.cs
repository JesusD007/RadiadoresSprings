namespace IntegrationApp.Contracts.Requests.Pagos;

public record SimularPagoRequest
{
    public int OrdenId { get; init; }
    public decimal Monto { get; init; }
    public string MetodoPago { get; init; } = string.Empty;  // "Tarjeta" | "PayPal"
    public string? TokenPago { get; init; }
}

public record RegistrarPagoRequest(
    int ClienteId, int? CuentaCobrarId, decimal Monto,
    string MetodoPago, string? Referencia, string? Notas);
