namespace IntegrationApp.Contracts.Requests.Clientes;

public record CrearClienteRequest
{
    public string Nombre { get; init; } = string.Empty;
    public string? Apellido { get; init; }
    public string? Email { get; init; }
    public string? Telefono { get; init; }
    public string? Direccion { get; init; }
    public string? RFC { get; init; }
    public string? Tipo { get; init; } = "Regular";
    public decimal LimiteCredito { get; init; }
}
