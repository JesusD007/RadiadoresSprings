using IntegrationApp.Contracts.Requests.Caja;
using IntegrationApp.Contracts.Responses.Caja;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/caja")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CajaController(ICoreApiClient core, ICircuitBreakerStateService cbState)
    {
        _core = core;
        _cbState = cbState;
    }

    private string Token => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    private IActionResult Unavailable() =>
        StatusCode(503, new { error = "Operación de caja requiere conexión con el Core" });

    /// <summary>[CRÍTICO] Apertura de caja con monto inicial.</summary>
    [HttpPost("inicio-dia")]
    public async Task<IActionResult> InicioDia(
        [FromBody] InicioDiaRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable) return Unavailable();

        var response = await _core.PostAsync("/api/v1/caja/inicio-dia", request, bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<InicioDiaResponse>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }

    /// <summary>Entrada o salida de efectivo con motivo y firma digital.</summary>
    [HttpPost("movimiento")]
    public async Task<IActionResult> Movimiento(
        [FromBody] MovimientoCajaRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable) return Unavailable();

        var response = await _core.PostAsync("/api/v1/caja/movimiento", request, bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<MovimientoCajaResponse>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }

    /// <summary>Cuadre de caja y cierre de sesión.</summary>
    [HttpPost("cierre-dia")]
    public async Task<IActionResult> CierreDia(
        [FromBody] CierreDiaRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable) return Unavailable();

        var response = await _core.PostAsync("/api/v1/caja/cierre-dia", request, bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<CierreDiaResponse>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
