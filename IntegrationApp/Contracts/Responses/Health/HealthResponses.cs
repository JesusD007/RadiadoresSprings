namespace IntegrationApp.Contracts.Responses.Health;

public record HealthResponse
{
    public string Status { get; init; } = string.Empty;         // "Healthy" | "Degraded" | "Unhealthy"
    public bool CoreDisponible { get; init; }
    public bool MirrorActualizado { get; init; }
    public int ColasPendientes { get; init; }
    public DateTimeOffset UltimaSync { get; init; }
    public Dictionary<string, string> Checks { get; init; } = new();
}
