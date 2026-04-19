namespace IntegrationApp.Contracts.Requests.Ventas;

public record CrearVentaRequest
{
    public Guid ClienteId { get; init; }
    public string CajeroId { get; init; } = string.Empty;
    public string SucursalId { get; init; } = string.Empty;
    public string MetodoPago { get; init; } = string.Empty;  // "Efectivo" | "Tarjeta" | "Transferencia"
    public decimal MontoRecibido { get; init; }
    public IReadOnlyList<LineaVentaDto> Lineas { get; init; } = [];
}

public record LineaVentaDto
{
    public int ProductoId { get; init; }
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal? Descuento { get; init; }
}
