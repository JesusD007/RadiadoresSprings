namespace Core.Domain.Entities;

public class LineaOrden
{
    public int Id { get; set; }
    public int OrdenId { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => PrecioUnitario * Cantidad;

    // Navegación
    public Orden Orden { get; set; } = null!;
    public Producto Producto { get; set; } = null!;
}
