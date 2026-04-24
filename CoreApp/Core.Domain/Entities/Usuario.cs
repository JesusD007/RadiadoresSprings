using Core.Domain.Enums;

namespace Core.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; }
    public int SucursalId { get; set; }
    public bool EsActivo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// ID del cliente asociado. Solo aplica para usuarios con rol Cliente.
    /// Se asigna al registrar un cliente web (RegistrarClienteWebAsync).
    /// </summary>
    public int? ClienteId { get; set; }

    // Navegación
    public Sucursal Sucursal { get; set; } = null!;
    public Cliente? Cliente { get; set; }
    public ICollection<SesionCaja> SesionesCaja { get; set; } = [];
}
