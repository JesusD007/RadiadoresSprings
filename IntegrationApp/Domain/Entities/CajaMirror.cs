namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local de Cajas. Solo lectura offline.
/// </summary>
public class CajaMirror
{
    public int CoreId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int SucursalId { get; set; }
    public bool EsActiva { get; set; }
    public DateTime UltimaSync { get; set; }
}
