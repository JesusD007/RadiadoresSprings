namespace IntegrationApp.Domain.Entities;

/// <summary>
/// Réplica local de Categorias. Solo lectura offline.
/// </summary>
public class CategoriaMirror
{
    public int CoreId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsActivo { get; set; }
    public DateTime UltimaSync { get; set; }
}
