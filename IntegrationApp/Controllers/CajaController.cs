using IntegrationApp.Contracts.Requests.Caja;
using IntegrationApp.Contracts.Responses.Caja;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/caja")]
[Authorize]
public class CajaController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly ILogger<CajaController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CajaController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        IntegrationDbContext db,
        ILogger<CajaController> logger)
    {
        _core = core;
        _cbState = cbState;
        _db = db;
        _logger = logger;
    }

    private string Token   => Request.Headers.Authorization.ToString().Replace("Bearer ", "");
    private string UserId  => User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0";
    private string UserName => User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Desconocido";

    // ── POST /api/v1/caja/inicio-dia ──────────────────────────────────────────

    /// <summary>
    /// Apertura de sesión de caja.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: crea SesionCajaMirror local + encola en OperacionPendiente.
    /// </summary>
    [HttpPost("inicio-dia")]
    public async Task<IActionResult> InicioDia(
        [FromBody] InicioDiaRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/caja/inicio-dia", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(JsonSerializer.Deserialize<InicioDiaResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Caja] InicioDia offline — sucursal={Suc}", request.SucursalId);

        // Verificar que no haya una sesión ya abierta para este cajero
        var sesionAbierta = await _db.SesionesCajaMirror
            .AnyAsync(s => s.NombreUsuario == UserId && s.Estado == "Abierta", ct);
        if (sesionAbierta)
        {
            return Conflict(new
            {
                error   = "Ya existe una sesión de caja abierta para este cajero",
                offline = true
            });
        }

        var idLocal = Guid.NewGuid();
        int.TryParse(UserId, out int usuarioIdInt);

        var sesion = new SesionCajaMirror
        {
            IdLocal       = idLocal,
            CajaId        = 0,                        // placeholder — se actualizará al sincronizar
            NombreCaja    = request.SucursalId,
            UsuarioId     = usuarioIdInt,
            NombreUsuario = UserName,
            MontoApertura = request.MontoInicial,
            Estado        = "Abierta",
            FechaApertura = request.Fecha == default ? DateTimeOffset.UtcNow : request.Fecha,
            EstadoSync    = "Pendiente"
        };
        _db.SesionesCajaMirror.Add(sesion);

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Caja",
            TipoOperacion   = "AbrirSesion",
            EndpointCore    = "/api/v1/caja/inicio-dia",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = idLocal.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Caja] Sesión offline abierta — IdLocal={Id}", idLocal);

        return Ok(new InicioDiaResponse
        {
            SesionCajaId = idLocal,
            Estado       = "Abierta",
            Inicio       = sesion.FechaApertura
        });
    }

    // ── POST /api/v1/caja/movimiento ──────────────────────────────────────────

    /// <summary>
    /// Entrada o salida de efectivo.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: encola en OperacionPendiente con el IdLocal de la sesión.
    /// </summary>
    [HttpPost("movimiento")]
    public async Task<IActionResult> Movimiento(
        [FromBody] MovimientoCajaRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/caja/movimiento", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(JsonSerializer.Deserialize<MovimientoCajaResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Caja] Movimiento offline — sesión={Ses}, tipo={Tipo}",
            request.SesionCajaId, request.Tipo);

        // Verificar que la sesión exista localmente
        var sesion = await _db.SesionesCajaMirror
            .FirstOrDefaultAsync(s => s.IdLocal == request.SesionCajaId && s.Estado == "Abierta", ct);

        if (sesion is null)
        {
            return NotFound(new
            {
                error   = "Sesión de caja no encontrada o no está abierta",
                offline = true
            });
        }

        var movimientoId = Guid.NewGuid();

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Caja",
            TipoOperacion   = "Movimiento",
            EndpointCore    = "/api/v1/caja/movimiento",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = movimientoId.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new MovimientoCajaResponse
        {
            MovimientoId = movimientoId,
            SaldoActual  = 0,        // No calculamos el saldo offline (sin contabilidad local)
            FechaHora    = DateTimeOffset.UtcNow
        });
    }

    // ── POST /api/v1/caja/cierre-dia ─────────────────────────────────────────

    /// <summary>
    /// Cierre de sesión de caja.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: actualiza SesionCajaMirror + encola cierre en OperacionPendiente.
    /// </summary>
    [HttpPost("cierre-dia")]
    public async Task<IActionResult> CierreDia(
        [FromBody] CierreDiaRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/caja/cierre-dia", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(JsonSerializer.Deserialize<CierreDiaResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Caja] CierreDia offline — sesión={Ses}", request.SesionCajaId);

        var sesion = await _db.SesionesCajaMirror
            .FirstOrDefaultAsync(s => s.IdLocal == request.SesionCajaId && s.Estado == "Abierta", ct);

        if (sesion is null)
        {
            return NotFound(new
            {
                error   = "Sesión de caja no encontrada o ya está cerrada",
                offline = true
            });
        }

        // Marcar como cerrada localmente
        sesion.Estado        = "Cerrada";
        sesion.MontoCierre   = request.MontoContadoEfectivo;
        sesion.FechaCierre   = DateTimeOffset.UtcNow;
        sesion.Observaciones = "Cierre registrado en modo offline";

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Caja",
            TipoOperacion   = "CerrarSesion",
            EndpointCore    = "/api/v1/caja/cierre-dia",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = request.SesionCajaId.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new CierreDiaResponse
        {
            MontoSistema  = sesion.MontoApertura,           // Aproximado sin contabilidad offline
            MontoContado  = request.MontoContadoEfectivo,
            Diferencia    = request.MontoContadoEfectivo - sesion.MontoApertura,
            CuadreCorrecto = false,     // No podemos confirmar el cuadre sin Core
            PorMetodo     = []
        });
    }
}
