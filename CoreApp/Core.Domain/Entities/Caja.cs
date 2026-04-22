namespace Core.Domain.Entities;

public class Caja
{
    public int Id { get; set; }
    public int SucursalId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool EsActiva { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación
    public Sucursal Sucursal { get; set; } = null!;
    public ICollection<SesionCaja> Sesiones { get; set; } = [];
}
