using IntegrationApp.Contracts.Requests.Clientes;
using IntegrationApp.Contracts.Responses.Clientes;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Helpers;
using IntegrationApp.Mappings;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/clientes")]
[Authorize]
public class ClientesController : ControllerBase
{
    private static readonly Guid AnonimoCLIENTEId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly ILogger<ClientesController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public ClientesController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        IntegrationDbContext db,
        ILogger<ClientesController> logger)
    {
        _core = core;
        _cbState = cbState;
        _db = db;
        _logger = logger;
    }

    private string Token  => Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    private string UserId => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

    // ── GET /api/v1/clientes/{id} ─────────────────────────────────────────────

    /// <summary>
    /// Datos del cliente por LocalId (Guid).
    /// ONLINE:  proxy al Core usando CoreId.
    /// OFFLINE: servido desde ClienteMirror.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClienteDto>> GetCliente(Guid id, CancellationToken ct)
    {
        // ── MODO ONLINE ────────────────────────────────────────────────────────
        if (_cbState.CoreAvailable)
        {
            // Resolver CoreId (int) a partir del LocalId (Guid) del mirror
            var mirror = await _db.ClientesMirror
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.LocalId == id, ct);

            string endpoint = mirror?.CoreId is int coreId
                ? $"/api/v1/clientes/{coreId}"
                : $"/api/v1/clientes/{id}";

            var response = await _core.GetAsync(endpoint, bearerToken: Token, ct: ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return Ok(ProxyHelper.Unwrap<ClienteDto>(content, _json));
            }

            return response.StatusCode == System.Net.HttpStatusCode.NotFound
                ? Ok(ClienteAnonimo())
                : StatusCode((int)response.StatusCode);
        }

        // ── MODO OFFLINE: servir desde ClienteMirror ───────────────────────────
        _logger.LogWarning("[Clientes] Offline — sirviendo desde mirror id={Id}", id);

        var clienteMirror = await _db.ClientesMirror
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LocalId == id && c.EsActivo, ct);

        return clienteMirror is not null
            ? Ok(MapToDto(clienteMirror))
            : Ok(ClienteAnonimo());
    }

    // ── GET /api/v1/clientes/buscar?q=... ─────────────────────────────────────

    /// <summary>
    /// Buscar clientes por nombre, apellido, email o RFC.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: búsqueda en ClienteMirror.
    /// </summary>
    [HttpGet("buscar")]
    public async Task<ActionResult<IReadOnlyList<ClienteDto>>> Buscar(
        [FromQuery] string q, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.GetAsync(
                $"/api/v1/clientes/buscar?q={Uri.EscapeDataString(q)}", bearerToken: Token, ct: ct);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                return Ok(ProxyHelper.Unwrap<IReadOnlyList<ClienteDto>>(content, _json));
            }

            return StatusCode((int)response.StatusCode);
        }

        var term = q.ToLower();
        var resultados = await _db.ClientesMirror
            .AsNoTracking()
            .Where(c => c.EsActivo && (
                c.Nombre.ToLower().Contains(term) ||
                (c.Apellido != null && c.Apellido.ToLower().Contains(term)) ||
                (c.Email    != null && c.Email.ToLower().Contains(term))    ||
                (c.RFC      != null && c.RFC.ToLower().Contains(term))))
            .Take(50)
            .ToListAsync(ct);

        return Ok(resultados.Select(MapToDto).ToList());
    }

    // ── POST /api/v1/clientes ──────────────────────────────────────────────────

    /// <summary>
    /// Crear cliente.
    /// ONLINE:  proxy al Core — fuente de verdad.
    /// OFFLINE: crea en ClienteMirror (EsLocal=true) y encola en OperacionPendiente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CrearCliente(
        [FromBody] CrearClienteRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/clientes", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Clientes] Creando cliente offline — usuario {User}", UserId);

        var localId = Guid.NewGuid();

        _db.ClientesMirror.Add(new ClienteMirror
        {
            LocalId        = localId,
            CoreId         = null,
            Nombre         = request.Nombre,
            Apellido       = request.Apellido,
            Email          = request.Email,
            Telefono       = request.Telefono,
            Direccion      = request.Direccion,
            RFC            = request.RFC,
            Tipo           = request.Tipo ?? "Regular",
            LimiteCredito  = request.LimiteCredito,
            SaldoPendiente = 0,
            EsActivo       = true,
            EsLocal        = true,
            UltimaSync     = DateTime.UtcNow
        });

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Cliente",
            TipoOperacion   = "Crear",
            EndpointCore    = "/api/v1/clientes",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = localId.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Clientes] Cliente offline creado — LocalId={Id}", localId);

        return Accepted(new
        {
            localId = localId,
            offline = true,
            message = "Cliente creado localmente. Se sincronizará cuando el sistema central esté disponible.",
            nombre  = $"{request.Nombre} {request.Apellido}".Trim()
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ClienteDto ClienteAnonimo() => new()
    {
        Id             = AnonimoCLIENTEId,
        EsAnonimo      = true,
        Nombre         = "Cliente Anónimo",
        LimiteCredito  = 0,
        SaldoPendiente = 0
    };

    private static ClienteDto MapToDto(ClienteMirror c) => new()
    {
        Id             = c.LocalId,
        CoreId         = c.CoreId,
        Nombre         = c.Nombre,
        Apellido       = c.Apellido,
        Email          = c.Email,
        Telefono       = c.Telefono,
        Direccion      = c.Direccion,
        RFC            = c.RFC,
        Tipo           = c.Tipo,
        LimiteCredito  = c.LimiteCredito,
        SaldoPendiente = c.SaldoPendiente,
        EsActivo       = c.EsActivo,
        EsLocal        = c.EsLocal,
        Offline        = true
    };
}
