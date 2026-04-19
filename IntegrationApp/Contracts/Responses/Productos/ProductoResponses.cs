namespace IntegrationApp.Contracts.Responses.Productos;

public record ProductoPagedResult
{
    public IReadOnlyList<ProductoResumenDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool FromMirror { get; init; }
}

public record ProductoResumenDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Stock { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public bool EsActivo { get; init; }
}

public record ProductoDetalleDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string? Descripcion { get; init; }
    public decimal Precio { get; init; }
    public decimal? PrecioOferta { get; init; }
    public int Stock { get; init; }
    public int StockMinimo { get; init; }
    public int CategoriaId { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public bool EsActivo { get; init; }
    public DateTimeOffset UltimaSync { get; init; }
    public bool FromMirror { get; init; }
}
