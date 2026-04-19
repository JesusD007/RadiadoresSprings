using IntegrationApp.Contracts.Responses.CuentasCobrar;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/cuentascobrar")]
[Authorize]
public class CuentasCobrarController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CuentasCobrarController(ICoreApiClient core, ICircuitBreakerStateService cbState)
    {
        _core = core;
        _cbState = cbState;
    }

    private string Token => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    /// <summary>Cuentas por cobrar pendientes del cliente con abonos. Proxy hacia Core.</summary>
    [HttpGet("{clienteId:guid}")]
    public async Task<ActionResult<IReadOnlyList<CuentaPorCobrarDto>>> GetCuentasCobrar(
        Guid clienteId, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio no disponible temporalmente" });

        var response = await _core.GetAsync($"/api/v1/cuentascobrar/{clienteId}", bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<IReadOnlyList<CuentaPorCobrarDto>>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
