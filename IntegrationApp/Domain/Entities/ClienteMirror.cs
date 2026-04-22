namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local del catálogo de clientes del Core.
/// FUENTE DE VERDAD: Core.API (SQL Server). Este mirror es solo fallback de lectura.
/// Clientes creados offline se almacenan en OperacionPendiente y se sincronizan
/// cuando Core vuelve. El ID local (negativo o GUID) se sustituye por el del Core
/// al sincronizar.
/// </summary>
public class ClienteMirror
{
    /// <summary>PK del Core. Si el cliente fue creado offline, este campo está vacío hasta sync.</summary>
    public int? CoreId { get; set; }

    /// <summary>ID local generado offline (usado como PK hasta que Core asigne el definitivo).</summary>
    public Guid LocalId { get; set; } = Guid.NewGuid();

    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
    public string? RFC { get; set; }
    public string Tipo { get; set; } = "Regular";
    public decimal LimiteCredito { get; set; }
    public decimal SaldoPendiente { get; set; }
    public bool EsActivo { get; set; } = true;

    /// <summary>True si este cliente fue creado offline y aún no sincronizado con Core.</summary>
    public bool EsLocal { get; set; } = false;

    public DateTime UltimaSync { get; set; } = DateTime.UtcNow;
}
