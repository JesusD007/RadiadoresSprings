namespace IntegrationApp.Contracts.Requests.Ordenes;

public record CrearOrdenRequest
{
    public Guid ClienteId { get; init; }
    public string? Notas { get; init; }
    public DireccionEntregaDto? Entrega { get; init; }
    public IReadOnlyList<LineaOrdenDto> Lineas { get; init; } = [];
}

public record DireccionEntregaDto
{
    public string Calle { get; init; } = string.Empty;
    public string Ciudad { get; init; } = string.Empty;
    public string? Provincia { get; init; }
    public string? CodigoPostal { get; init; }
}

public record LineaOrdenDto
{
    public int ProductoId { get; init; }
    public int Cantidad { get; init; }
}
