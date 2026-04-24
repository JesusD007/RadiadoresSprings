using IntegrationApp.Contracts.Requests.Ordenes;
using IntegrationApp.Contracts.Responses.Ordenes;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Helpers;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/ordenes")]
[Authorize]
public class OrdenesController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly ILogger<OrdenesController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public OrdenesController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        IntegrationDbContext db,
        ILogger<OrdenesController> logger)
    {
        _core = core;
        _cbState = cbState;
        _db = db;
        _logger = logger;
    }

    private string Token  => Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    // ── POST /api/v1/ordenes ──────────────────────────────────────────────────

    /// <summary>
    /// Crear orden desde portal web.
    /// ONLINE:  proxy al Core (respuesta 202 con PollUrl).
    /// OFFLINE: encola en OperacionPendiente, retorna 202 con tracking local.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrdenCreadaResponse>> CrearOrden(
        [FromBody] CrearOrdenRequest request, CancellationToken ct)
    {
        // ── MODO ONLINE ────────────────────────────────────────────────────────
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/ordenes", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var orden = ProxyHelper.Unwrap<OrdenCreadaResponse>(content, _json)!;
                var pollUrl = $"{Request.Scheme}://{Request.Host}/api/v1/ordenes/{orden.OrdenId}/estado";
                return Accepted(new OrdenCreadaResponse
                {
                    OrdenId = orden.OrdenId,
                    Estado  = orden.Estado,
                    Total   = orden.Total,
                    PollUrl = pollUrl
                });
            }

            return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Ordenes] Creando orden offline — usuario={User}", UserId);

        var idLocal = Guid.NewGuid();

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Orden",
            TipoOperacion   = "Crear",
            EndpointCore    = "/api/v1/ordenes",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = idLocal.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Ordenes] Orden offline encolada — IdLocal={Id}", idLocal);

        return Accepted(new OrdenCreadaResponse
        {
            OrdenId = -Random.Shared.Next(1, 1000000),
            Estado  = "PendienteSync",
            Total   = request.Lineas.Sum(l => l.Cantidad * 0m), // total desconocido offline
            PollUrl = $"{Request.Scheme}://{Request.Host}/api/v1/ordenes/{idLocal}/estado"
        });
    }

    // ── GET /api/v1/ordenes/{id}/estado ──────────────────────────────────────

    /// <summary>
    /// Seguimiento de orden.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: retorna estado "PendienteSync" para órdenes encoladas localmente.
    /// </summary>
    [HttpGet("{id:guid}/estado")]
    [AllowAnonymous]
    public async Task<ActionResult<EstadoOrdenDto>> GetEstado(Guid id, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.GetAsync($"/api/v1/ordenes/{id}/estado", bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(ProxyHelper.Unwrap<EstadoOrdenDto>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // Offline: verificar si la orden está encolada localmente
        var encolada = await _db.OperacionesPendientes
            .AnyAsync(op => op.IdLocalTemporal == id.ToString()
                         && op.TipoEntidad == "Orden", ct);

        if (!encolada)
            return NotFound(new { error = "Orden no encontrada", offline = true });

        return Ok(new EstadoOrdenDto
        {
            OrdenId  = -Random.Shared.Next(1, 1000000),
            Estado   = "PendienteSync",
            Mensaje  = "Orden registrada localmente. Se procesará cuando el sistema central esté disponible.",
            Offline  = true
        });
    }
}
