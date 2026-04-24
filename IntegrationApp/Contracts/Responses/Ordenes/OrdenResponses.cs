namespace IntegrationApp.Contracts.Responses.Ordenes;

public record OrdenCreadaResponse
{
    public int OrdenId { get; init; }
    public string Estado { get; init; } = "Pendiente";
    public decimal Total { get; init; }
    public string PollUrl { get; init; } = string.Empty;
}

public record EstadoOrdenDto
{
    public int OrdenId { get; init; }
    public string Estado { get; init; } = string.Empty;  // "Pendiente" | "Procesando" | "Listo" | "Entregado" | "Cancelado" | "PendienteSync"
    public string? Mensaje { get; init; }
    public bool Offline { get; init; }
    public DateTimeOffset CreadaEn { get; init; }
    public DateTimeOffset? ActualizadaEn { get; init; }
    public IReadOnlyList<HistorialEstadoDto> Historial { get; init; } = [];
}

public record HistorialEstadoDto
{
    public string Estado { get; init; } = string.Empty;
    public DateTimeOffset Fecha { get; init; }
    public string? Nota { get; init; }
}

public record OrdenResponse(
    int Id, string NumeroOrden, int ClienteId, string NombreCliente,
    string Estado, DateTime Fecha, DateTime? FechaEntrega, decimal TotalOrden,
    int CantidadProductos,
    string MetodoPago, string? DireccionEnvio,
    List<LineaOrdenResponse> Lineas);

public record LineaOrdenResponse(
    int Id, int ProductoId, string CodigoProducto, string NombreProducto,
    int Cantidad, decimal PrecioUnitario, decimal Subtotal);
