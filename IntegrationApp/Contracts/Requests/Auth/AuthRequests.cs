using System.ComponentModel.DataAnnotations;

namespace IntegrationApp.Contracts.Requests.Auth;

public record LoginRequest
{
    [Required] public string Username { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
    public string? Rol { get; init; } // "Cajero" | "Admin" | "Cliente"
}

public record RefreshRequest
{
    [Required] public string RefreshToken { get; init; } = string.Empty;
}
