using IntegrationApp.Contracts.Requests.Auth;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ILocalAuthService _localAuth;
    private readonly ILogger<AuthController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AuthController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        ILocalAuthService localAuth,
        ILogger<AuthController> logger)
    {
        _core = core;
        _cbState = cbState;
        _localAuth = localAuth;
        _logger = logger;
    }

    /// <summary>
    /// Autenticar usuario.
    /// MODO ONLINE:  proxy al Core — fuente de verdad absoluta.
    /// MODO OFFLINE: valida contra UsuarioMirror con BCrypt; emite JWT con las mismas claves del Core.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        // ── MODO ONLINE: Core disponible → siempre delegar al Core ─────────────
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/auth/login", request, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.OK           => Ok(JsonSerializer.Deserialize<object>(content, _json)),
                System.Net.HttpStatusCode.Unauthorized => Unauthorized(JsonSerializer.Deserialize<object>(content, _json)),
                System.Net.HttpStatusCode.Locked       => StatusCode(423, JsonSerializer.Deserialize<object>(content, _json)),
                _ => StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json))
            };
        }

        // ── MODO OFFLINE: Core no disponible → autenticación local ─────────────
        _logger.LogWarning("[Auth] Core no disponible — autenticación offline para '{User}'", request.Username);

        var resultado = await _localAuth.LoginAsync(request.Username, request.Password);

        if (resultado is null)
            return Unauthorized(new { error = "Credenciales inválidas", offline = true });

        return Ok(new
        {
            token     = resultado.Token,
            expiresAt = resultado.ExpiresAt,
            rol       = resultado.Rol,
            nombre    = resultado.NombreCompleto,
            offline   = true,
            message   = "Sesión iniciada en modo offline. Algunas funciones pueden estar limitadas."
        });
    }

    /// <summary>
    /// Renovar token.
    /// MODO OFFLINE: no disponible — el usuario debe volver a hacer login offline.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
        {
            return StatusCode(503, new
            {
                error   = "Renovación de token no disponible en modo offline",
                offline = true,
                hint    = "Por favor inicie sesión nuevamente"
            });
        }

        var response = await _core.PostAsync("/api/v1/auth/refresh", request, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<object>(content, _json))
            : Unauthorized(JsonSerializer.Deserialize<object>(content, _json));
    }

    /// <summary>
    /// Registro web.
    /// MODO OFFLINE: no disponible.
    /// </summary>
    [HttpPost("registro")]
    public async Task<IActionResult> RegistroWeb([FromBody] RegistroWebRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
        {
            return StatusCode(503, new
            {
                error   = "Registro no disponible en modo offline",
                offline = true,
                hint    = "Por favor intente más tarde"
            });
        }

        var response = await _core.PostAsync("/api/v1/auth/registro", request, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
