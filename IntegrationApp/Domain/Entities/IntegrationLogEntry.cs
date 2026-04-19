namespace IntegrationApp.Domain.Entities;

public class IntegrationLogEntry
{
    public long Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;   // "IN" | "OUT"
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public int HttpStatus { get; set; }
    public int LatenciaMs { get; set; }
    public bool DesdeCache { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string Layer { get; set; } = "Integracion";
    public DateTimeOffset Fecha { get; set; }
}
