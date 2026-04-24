namespace IntegrationApp.Contracts.Responses.Auth;

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }  // 3600 segundos
    public string Rol { get; init; } = string.Empty;
    public int UserId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public int? SucursalId { get; init; }
}

public record RefreshResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
}
