namespace IntegrationApp.Contracts.Responses.Clientes;

public record ClienteDto
{
    public Guid Id { get; init; }
    public int? CoreId { get; init; }          // ID en Core (null si aún no sincronizado)
    public bool EsAnonimo { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? Apellido { get; init; }
    public string? RncCedula { get; init; }
    public string? RFC { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public string? Direccion { get; init; }
    public string? Tipo { get; init; }
    public decimal LimiteCredito { get; init; }
    public decimal SaldoPendiente { get; init; }
    public bool EsActivo { get; init; } = true;
    public bool EsLocal { get; init; }         // true si fue creado offline aún no sincronizado
    public bool Offline { get; init; }         // true si la respuesta proviene del mirror local
}
