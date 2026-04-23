namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local de Sucursales. Solo lectura offline.
/// </summary>
public class SucursalMirror
{
    public int CoreId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public bool EsActivo { get; set; }
    public DateTime UltimaSync { get; set; }
}
