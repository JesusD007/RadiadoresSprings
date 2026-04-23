using IntegrationApp.Contracts.Requests.Pagos;
using IntegrationApp.Contracts.Responses.Pagos;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Helpers;
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
    private readonly IntegrationDbContext _db;
    private readonly ILogger<PagosController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public PagosController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        IntegrationDbContext db,
        ILogger<PagosController> logger)
    {
        _core = core;
        _cbState = cbState;
        _db = db;
        _logger = logger;
    }

    private string Token  => Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    /// <summary>
    /// Simular pago con procesador externo.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: encola en OperacionPendiente — el procesador real se invocará al recuperar Core.
    ///
    /// NOTA: Los pagos offline tienen riesgo de duplicado si el usuario reintenta
    /// manualmente. El Idempotency-Key garantiza que Core descarte duplicados al sincronizar.
    /// </summary>
    [HttpPost("simular")]
    public async Task<ActionResult<SimularPagoResponse>> SimularPago(
        [FromBody] SimularPagoRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/pagos/simular", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(ProxyHelper.Unwrap<SimularPagoResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Pagos] Pago offline — orden={Orden}, monto={Monto}, método={Met}",
            request.OrdenId, request.Monto, request.MetodoPago);

        var localId = Guid.NewGuid();

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Pago",
            TipoOperacion   = "SimularPago",
            EndpointCore    = "/api/v1/pagos/simular",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = localId.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Pagos] Pago offline encolado — IdLocal={Id}", localId);

        return Accepted(new SimularPagoResponse
        {
            TransaccionId = localId,
            Estado        = "PendienteSync",
            Monto         = request.Monto,
            MetodoPago    = request.MetodoPago,
            Offline       = true,
            Mensaje       = "Pago registrado localmente. Se procesará cuando el sistema central esté disponible."
        });
    }
}
