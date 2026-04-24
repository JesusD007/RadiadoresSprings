namespace IntegrationApp.Contracts.Responses.Ventas;

public record VentaResponse
{
    public int FacturaId { get; init; }
    public string NumeroFactura { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal Itbis { get; init; }
    public decimal Total { get; init; }
    public decimal Cambio { get; init; }
    public DateTimeOffset FechaHora { get; init; }
    public string Estado { get; init; } = string.Empty;     // "Confirmada" | "Pendiente"
    public bool DesdeOffline { get; init; }
}
