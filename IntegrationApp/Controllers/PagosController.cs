using IntegrationApp.Contracts.Requests.Pagos;
using IntegrationApp.Contracts.Responses.Pagos;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/pagos")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ILogger<PagosController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PagosController(ICoreApiClient core, ICircuitBreakerStateService cbState, ILogger<PagosController> logger)
    {
        _core = core;
        _cbState = cbState;
        _logger = logger;
    }

    private string Token => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    /// <summary>Simular pago con procesador externo vía Integración.</summary>
    [HttpPost("simular")]
    public async Task<ActionResult<SimularPagoResponse>> SimularPago(
        [FromBody] SimularPagoRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio de pagos no disponible temporalmente" });

        var response = await _core.PostAsync("/api/v1/pagos/simular", request, bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<SimularPagoResponse>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
