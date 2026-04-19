namespace IntegrationApp.Contracts.Responses.Clientes;

public record ClienteDto
{
    public Guid Id { get; init; }
    public bool EsAnonimo { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? RncCedula { get; init; }
    public string? Telefono { get; init; }
    public string? Email { get; init; }
    public decimal LimiteCredito { get; init; }
    public decimal SaldoPendiente { get; init; }
}
