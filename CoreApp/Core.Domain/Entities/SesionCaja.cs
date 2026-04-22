using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class SesionCaja
{
    public int Id { get; set; }
    public int CajaId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
    public DateTime? FechaCierre { get; set; }
    public decimal MontoApertura { get; set; }
    public decimal? MontoCierre { get; set; }
    public decimal? MontoSistema { get; set; }
    public decimal? Diferencia { get; set; }
    public EstadoSesionCaja Estado { get; set; } = EstadoSesionCaja.Abierta;
    public string? Observaciones { get; set; }

    // Navegación
    public Caja Caja { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<Venta> Ventas { get; set; } = [];

    // Métodos de dominio
    public void Cerrar(decimal montoCierre, decimal montoSistema)
    {
        FechaCierre = DateTime.UtcNow;
        MontoCierre = montoCierre;
        MontoSistema = montoSistema;
        Diferencia = montoCierre - montoSistema;
        Estado = Math.Abs(Diferencia.Value) < 0.01m
            ? EstadoSesionCaja.Cuadrada
            : EstadoSesionCaja.Cerrada;
    }
}
