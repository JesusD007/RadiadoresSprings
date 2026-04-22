namespace IntegrationApp.Contracts.Requests.CuentasCobrar;

public record RegistrarAbonoRequest
{
    public Guid CuentaId { get; init; }
    public decimal Monto { get; init; }
    public string MetodoPago { get; init; } = string.Empty;   // "Efectivo" | "Transferencia" | "Tarjeta"
    public string? Referencia { get; init; }
    public DateTimeOffset Fecha { get; init; }
}
