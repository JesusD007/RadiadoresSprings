namespace IntegrationApp.Contracts.Requests.Ventas;

public record CrearVentaRequest
{
    public int ClienteId { get; init; }
    public int CajeroId { get; init; }
    public int SucursalId { get; init; }
    public int CajaId { get; init; }
    public int SesionCajaId { get; init; }
    public string MetodoPago { get; init; } = string.Empty;  // "Efectivo" | "Tarjeta" | "Transferencia"
    public decimal MontoRecibido { get; init; }
    public decimal Descuento { get; init; }
    public string? Observaciones { get; init; }
    public IReadOnlyList<LineaVentaDto> Lineas { get; init; } = [];
}

public record LineaVentaDto
{
    public int ProductoId { get; init; }
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal? Descuento { get; init; }
}
