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

public record RegistroWebRequest
{
    [Required] public string Username { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
    [Required] public string Nombre { get; init; } = string.Empty;
    [Required] public string Apellido { get; init; } = string.Empty;
    [Required] public string Email { get; init; } = string.Empty;
}
