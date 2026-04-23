namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local de los usuarios del Core necesaria para autenticar
/// cuando el Core no está disponible.
/// FUENTE DE VERDAD: Core.API (SQL Server). Este mirror es solo fallback.
/// Se sincroniza cada N minutos cuando Core está disponible.
/// </summary>
public class UsuarioMirror
{
    /// <summary>PK del Core (int auto-increment en SQL Server).</summary>
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    /// <summary>Hash BCrypt sincronizado desde Core. Nunca se modifica localmente.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Rol como string (ej. "Administrador", "ServicioWeb", "Cliente", "Cajero").</summary>
    public string Rol { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EsActivo { get; set; }

    public DateTime UltimaSync { get; set; } = DateTime.UtcNow;
}
