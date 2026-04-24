namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Sesión de caja gestionada localmente cuando el Core no está disponible.
/// FUENTE DE VERDAD: Core.API (SQL Server) cuando está activo.
/// En modo offline, el POS puede abrir y cerrar sesiones localmente.
/// Al recuperar el Core, las sesiones se sincronizan via OperacionPendiente.
/// </summary>
public class SesionCajaMirror
{
    public long Id { get; set; }

    /// <summary>GUID local generado al crear la sesión offline. Idempotency key al sincronizar.</summary>
    public int IdLocal { get; set; }

    /// <summary>ID de la caja (sincronizado desde Core via CajaMirror, o conocido de antemano).</summary>
    public int CajaId { get; set; }

    public string NombreCaja { get; set; } = string.Empty;

    /// <summary>ID del usuario (de UsuarioMirror).</summary>
    public int UsuarioId { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public decimal MontoApertura { get; set; }
    public decimal? MontoCierre { get; set; }

    /// <summary>"Abierta" | "Cerrada"</summary>
    public string Estado { get; set; } = "Abierta";

    public DateTimeOffset FechaApertura { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FechaCierre { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>"Pendiente" | "Sincronizada" | "Rechazada"</summary>
    public string EstadoSync { get; set; } = "Pendiente";

    /// <summary>ID asignado por Core una vez sincronizada. Null hasta entonces.</summary>
    public int? CoreSesionId { get; set; }
}
