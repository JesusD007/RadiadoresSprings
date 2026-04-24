namespace IntegrationApp.Contracts.Requests.Caja;

public record InicioDiaRequest
{
    public int SucursalId { get; init; }
    public int CajeroId { get; init; }
    public decimal MontoInicial { get; init; }
    public DateTimeOffset Fecha { get; init; }
}

public record MovimientoCajaRequest
{
    public int SesionCajaId { get; init; }
    public string Tipo { get; init; } = string.Empty;       // "IN" | "OUT"
    public decimal Monto { get; init; }
    public string Motivo { get; init; } = string.Empty;
    public string FirmaDigital { get; init; } = string.Empty;
}

public record CierreDiaRequest
{
    public int SesionCajaId { get; init; }
    public decimal MontoContadoEfectivo { get; init; }
}
