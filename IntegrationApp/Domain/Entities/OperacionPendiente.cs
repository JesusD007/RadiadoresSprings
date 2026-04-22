namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Cola genérica de operaciones de escritura realizadas offline.
/// Cuando Core vuelve, OperacionPendienteSyncService reproduce cada
/// operación en orden cronológico contra el endpoint correspondiente del Core.
///
/// INVARIANTE: Core siempre es la fuente de verdad. Cuando Core está disponible,
/// todas las escrituras van directo al Core — esta cola NUNCA se usa en modo online.
/// </summary>
public class OperacionPendiente
{
    public long Id { get; set; }

    /// <summary>
    /// GUID único por operación. Se envía como Idempotency-Key al Core
    /// para garantizar que los reintentos no generen duplicados.
    /// </summary>
    public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

    // ── Clasificación ────────────────────────────────────────────────────────

    /// <summary>"Cliente" | "Orden" | "Pago" | "CuentaCobrar" | "Caja" | "Producto"</summary>
    public string TipoEntidad { get; set; } = string.Empty;

    /// <summary>"Crear" | "Actualizar" | "Eliminar" | "CambiarEstado" | "AbrirSesion" | "CerrarSesion" | "Movimiento" | "AjustarStock"</summary>
    public string TipoOperacion { get; set; } = string.Empty;

    // ── Payload HTTP ─────────────────────────────────────────────────────────

    /// <summary>Endpoint del Core al que se debe hacer la llamada (ej. "/api/v1/clientes").</summary>
    public string EndpointCore { get; set; } = string.Empty;

    /// <summary>"POST" | "PUT" | "DELETE"</summary>
    public string MetodoHttp { get; set; } = "POST";

    /// <summary>Cuerpo JSON de la request tal y como debe enviarse al Core.</summary>
    public string PayloadJson { get; set; } = string.Empty;

    // ── Trazabilidad ─────────────────────────────────────────────────────────

    /// <summary>
    /// ID local temporal de la entidad creada offline (si aplica).
    /// Permite al cliente correlacionar la respuesta definitiva del Core.
    /// </summary>
    public string? IdLocalTemporal { get; set; }

    /// <summary>ID del usuario que realizó la operación (para auditoría).</summary>
    public string? UsuarioId { get; set; }

    // ── Estado de sincronización ──────────────────────────────────────────────

    /// <summary>"Pendiente" | "Sincronizada" | "Rechazada"</summary>
    public string Estado { get; set; } = "Pendiente";

    public DateTimeOffset FechaCreacion { get; set; } = DateTimeOffset.UtcNow;
    public int IntentosSync { get; set; } = 0;
    public DateTimeOffset? UltimoIntento { get; set; }
    public string? MotivoRechazo { get; set; }

    /// <summary>Respuesta del Core al sincronizar (para auditoría).</summary>
    public string? RespuestaCore { get; set; }
}
