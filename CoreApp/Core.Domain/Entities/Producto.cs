namespace Core.Domain.Entities;

public class Producto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public decimal? PrecioOferta { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; } = 5;
    public int CategoriaId { get; set; }
    public bool EsActivo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // Navegación
    public Categoria Categoria { get; set; } = null!;
    public ICollection<LineaVenta> LineasVenta { get; set; } = [];
    public ICollection<LineaOrden> LineasOrden { get; set; } = [];

    // Métodos de dominio
    public bool TieneStockBajo() => Stock <= StockMinimo;
    public decimal PrecioVigente() => PrecioOferta.HasValue && PrecioOferta > 0 ? PrecioOferta.Value : Precio;

    public bool ReducirStock(int cantidad)
    {
        if (Stock < cantidad) return false;
        Stock -= cantidad;
        FechaModificacion = DateTime.UtcNow;
        return true;
    }

    public void AumentarStock(int cantidad)
    {
        Stock += cantidad;
        FechaModificacion = DateTime.UtcNow;
    }
}
