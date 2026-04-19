namespace IntegrationApp.Contracts.Requests.Caja;

public record InicioDiaRequest
{
    public string SucursalId { get; init; } = string.Empty;
    public string CajeroId { get; init; } = string.Empty;
    public decimal MontoInicial { get; init; }
    public DateTimeOffset Fecha { get; init; }
}

public record MovimientoCajaRequest
{
    public Guid SesionCajaId { get; init; }
    public string Tipo { get; init; } = string.Empty;       // "IN" | "OUT"
    public decimal Monto { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public string FirmaDigital { get; init; } = string.Empty;
}

public record CierreDiaRequest
{
    public Guid SesionCajaId { get; init; }
    public decimal MontoContadoEfectivo { get; init; }
}
