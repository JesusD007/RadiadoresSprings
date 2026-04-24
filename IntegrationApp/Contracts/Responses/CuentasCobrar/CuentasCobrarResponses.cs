namespace IntegrationApp.Contracts.Responses.CuentasCobrar;

public record CuentaPorCobrarDto
{
    public int Id { get; init; }
    public int FacturaId { get; init; }
    public string NumeroFactura { get; init; } = string.Empty;
    public decimal MontoOriginal { get; init; }
    public decimal MontoPendiente { get; init; }
    public DateTimeOffset FechaVencimiento { get; init; }
    public string Estado { get; init; } = string.Empty;  // "Pendiente" | "Parcial" | "Pagada" | "Vencida"
    public IReadOnlyList<AbonoDto> Abonos { get; init; } = [];
}

public record AbonoDto
{
    public decimal Monto { get; init; }
    public string MetodoPago { get; init; } = string.Empty;
    public DateTimeOffset Fecha { get; init; }
}
