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
    private readonly ILogger<AuthController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public AuthController(ICoreApiClient core, ICircuitBreakerStateService cbState, ILogger<AuthController> logger)
    {
        _core = core;
        _cbState = cbState;
        _logger = logger;
    }

    /// <summary>Autenticar usuario. Proxy hacia Core. No almacena credenciales.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio de autenticación no disponible temporalmente" });

        var response = await _core.PostAsync("/api/v1/auth/login", request, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => Ok(JsonSerializer.Deserialize<object>(content, _json)),
            System.Net.HttpStatusCode.Unauthorized => Unauthorized(JsonSerializer.Deserialize<object>(content, _json)),
            System.Net.HttpStatusCode.Locked => StatusCode(423, JsonSerializer.Deserialize<object>(content, _json)),
            _ => StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json))
        };
    }

    /// <summary>Renovar token sin re-login. Proxy hacia Core.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio de autenticación no disponible temporalmente" });

        var response = await _core.PostAsync("/api/v1/auth/refresh", request, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<object>(content, _json))
            : Unauthorized(JsonSerializer.Deserialize<object>(content, _json));
    }
}
