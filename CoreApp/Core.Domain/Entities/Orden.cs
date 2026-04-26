using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Orden
{
    public int Id { get; set; }
    public string NumeroOrden { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public EstadoOrden Estado { get; set; } = EstadoOrden.Recibida;
    public DateTime Fecha { get; set; }
    public DateTime? FechaEstimadaEntrega { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public decimal TotalOrden { get; set; }
    public string? DireccionEnvio { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public string? Notas { get; set; }

    // Navegación
    public Cliente Cliente { get; set; } = null!;
    public ICollection<LineaOrden> Lineas { get; set; } = [];

    public void CambiarEstado(EstadoOrden nuevoEstado, DateTime ahora)
    {
        Estado = nuevoEstado;
        if (nuevoEstado == EstadoOrden.Entregada)
            FechaEntrega = ahora;
    }
}
