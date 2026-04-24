namespace IntegrationApp.Contracts.Responses.Caja;

public record InicioDiaResponse
{
    public int SesionCajaId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTimeOffset Inicio { get; init; }
}

public record MovimientoCajaResponse
{
    public int MovimientoId { get; init; }
    public decimal SaldoActual { get; init; }
    public DateTimeOffset FechaHora { get; init; }
}

public record CierreDiaResponse
{
    public decimal MontoSistema { get; init; }
    public decimal MontoContado { get; init; }
    public decimal Diferencia { get; init; }
    public bool CuadreCorrecto { get; init; }
    public IReadOnlyList<ResumenMetodoPagoDto> PorMetodo { get; init; } = [];
}

public record ResumenMetodoPagoDto
{
    public string MetodoPago { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public int Cantidad { get; init; }
}
