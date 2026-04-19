namespace IntegrationApp.Contracts.Responses.Ordenes;

public record OrdenCreadaResponse
{
    public Guid OrdenId { get; init; }
    public string Estado { get; init; } = "Pendiente";
    public decimal Total { get; init; }
    public string PollUrl { get; init; } = string.Empty;
}

public record EstadoOrdenDto
{
    public Guid OrdenId { get; init; }
    public string Estado { get; init; } = string.Empty;  // "Pendiente" | "Procesando" | "Listo" | "Entregado" | "Cancelado"
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
