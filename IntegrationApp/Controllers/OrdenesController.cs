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

    // DTO interno para mapear exactamente la respuesta de /api/v1/ordenes del Core
    private record CoreOrdenResponseDto(int Id, string Estado, decimal TotalOrden);

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
                var ordenCore = ProxyHelper.Unwrap<CoreOrdenResponseDto>(content, _json)!;
                var pollUrl = $"{Request.Scheme}://{Request.Host}/api/v1/ordenes/{ordenCore.Id}/estado";
                return Accepted(new OrdenCreadaResponse
                {
                    OrdenId = ordenCore.Id,
                    Estado  = ordenCore.Estado,
                    Total   = ordenCore.TotalOrden,
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
    /// OFFLINE: retorna estado "PendienteSync" para órdenes encoladas localmente o "Sincronizada" si ya se envió.
    /// </summary>
    [HttpGet("{id}/estado")]
    [AllowAnonymous]
    public async Task<ActionResult<EstadoOrdenDto>> GetEstado(string id, CancellationToken ct)
    {
        // 1. Tratar como ID numérico (Orden en Core)
        if (int.TryParse(id, out var coreId))
        {
            if (_cbState.CoreAvailable)
            {
                var response = await _core.GetAsync($"/api/v1/ordenes/{coreId}", bearerToken: Token, ct: ct);
                var content = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode)
                {
                    var ordenCore = ProxyHelper.Unwrap<CoreOrdenResponseDto>(content, _json);
                    if (ordenCore != null)
                    {
                        return Ok(new EstadoOrdenDto
                        {
                            OrdenId = ordenCore.Id,
                            Estado  = ordenCore.Estado,
                            Offline = false,
                            Mensaje = "Consultado en tiempo real en el sistema central."
                        });
                    }
                }
                return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
            }
            return StatusCode(503, new { error = "El sistema central no está disponible para consultar el estado actual." });
        }

        // 2. Tratar como GUID local (Orden Offline en OperacionesPendientes)
        var encolada = await _db.OperacionesPendientes
            .FirstOrDefaultAsync(op => op.IdLocalTemporal == id
                                    && op.TipoEntidad == "Orden"
                                    && op.TipoOperacion == "Crear", ct);

        if (encolada == null)
            return NotFound(new { error = "Orden no encontrada", offline = true });

        // Si ya se sincronizó, extraer el ID real que el Core le asignó
        if (encolada.Estado == "Sincronizada" && !string.IsNullOrWhiteSpace(encolada.RespuestaCore))
        {
            try
            {
                using var doc = JsonDocument.Parse(encolada.RespuestaCore);
                // La respuesta del Core es un ApiResponse<OrdenResponse> con el wrapper "data"
                if (doc.RootElement.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("id", out var idEl))
                {
                    var realCoreId = idEl.GetInt32();
                    var estadoReal = dataEl.TryGetProperty("estado", out var estEl) ? estEl.GetString() : "Sincronizada";
                    
                    return Ok(new EstadoOrdenDto
                    {
                        OrdenId = realCoreId,
                        Estado  = estadoReal ?? "Sincronizada",
                        Mensaje = "La orden fue sincronizada con éxito. Utiliza el nuevo OrdenId numérico para futuras consultas.",
                        Offline = false
                    });
                }
            }
            catch { /* Ignorar error de parseo y fallback genérico */ }

            return Ok(new EstadoOrdenDto { OrdenId = -1, Estado = "Sincronizada", Offline = false });
        }

        if (encolada.Estado == "Rechazada")
        {
            return Ok(new EstadoOrdenDto
            {
                OrdenId = -1,
                Estado  = "Rechazada",
                Mensaje = encolada.MotivoRechazo,
                Offline = true
            });
        }

        return Ok(new EstadoOrdenDto
        {
            OrdenId  = -1,
            Estado   = "PendienteSync",
            Mensaje  = "Orden registrada localmente. Se procesará cuando el sistema central esté disponible.",
            Offline  = true
        });
    }

    // ── GET /api/v1/ordenes/cliente/{clienteId} ─────────────────────────────

    /// <summary>
    /// Órdenes de un cliente.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: Retorna error de disponibilidad.
    /// </summary>
    [HttpGet("cliente/{clienteId:int}")]
    public async Task<ActionResult<IEnumerable<OrdenResponse>>> GetByCliente(int clienteId, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.GetAsync($"/api/v1/ordenes/cliente/{clienteId}", bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                // El Core devuelve un ApiResponse<IEnumerable<OrdenResponse>>
                return Ok(ProxyHelper.Unwrap<IEnumerable<OrdenResponse>>(content, _json));
            }

            return StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        return StatusCode(503, new { error = "El sistema central no está disponible para consultar el historial de órdenes del cliente." });
    }
}
