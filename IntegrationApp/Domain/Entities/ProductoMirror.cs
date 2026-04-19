namespace IntegrationApp.Domain.Entities;

public class ProductoMirror
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Stock { get; set; }
    public DateTime UltimaSync { get; set; }
    public bool EsActivo { get; set; }
    public string? Categoria { get; set; }
    public string? Descripcion { get; set; }
    public decimal? PrecioOferta { get; set; }
    public int StockMinimo { get; set; }
    public int CategoriaId { get; set; }
}
