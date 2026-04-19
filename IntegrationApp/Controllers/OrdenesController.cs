using IntegrationApp.Contracts.Requests.Ordenes;
using IntegrationApp.Contracts.Responses.Ordenes;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/ordenes")]
[Authorize]
public class OrdenesController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public OrdenesController(ICoreApiClient core, ICircuitBreakerStateService cbState)
    {
        _core = core;
        _cbState = cbState;
    }

    private string Token => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    /// <summary>[CRÍTICO] Crear orden desde portal web. Respuesta 202 (asíncrono).</summary>
    [HttpPost]
    public async Task<ActionResult<OrdenCreadaResponse>> CrearOrden(
        [FromBody] CrearOrdenRequest request, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio no disponible temporalmente" });

        var response = await _core.PostAsync("/api/v1/ordenes", request, bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
            response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var orden = JsonSerializer.Deserialize<OrdenCreadaResponse>(content, _json)!;
            // Enriquecer la PollUrl con la URL de Integración (no del Core directamente)
            var pollUrl = $"{Request.Scheme}://{Request.Host}/api/v1/ordenes/{orden.OrdenId}/estado";
            return Accepted(new OrdenCreadaResponse
            {
                OrdenId = orden.OrdenId,
                Estado = orden.Estado,
                Total = orden.Total,
                PollUrl = pollUrl
            });
        }

        return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }

    /// <summary>Seguimiento de orden por el cliente.</summary>
    [HttpGet("{id:guid}/estado")]
    [AllowAnonymous]  // Permite polling sin token para simplicidad en Website
    public async Task<ActionResult<EstadoOrdenDto>> GetEstado(Guid id, CancellationToken ct)
    {
        if (!_cbState.CoreAvailable)
            return StatusCode(503, new { error = "Servicio no disponible temporalmente" });

        var response = await _core.GetAsync($"/api/v1/ordenes/{id}/estado", bearerToken: Token, ct: ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        return response.IsSuccessStatusCode
            ? Ok(JsonSerializer.Deserialize<EstadoOrdenDto>(content, _json))
            : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
    }
}
