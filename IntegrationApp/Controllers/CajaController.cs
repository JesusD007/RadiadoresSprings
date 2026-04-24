using IntegrationApp.Contracts.Requests.Caja;
using IntegrationApp.Contracts.Responses.Caja;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Services;
using IntegrationApp.Helpers;
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

    // ── POST /api/v1/caja/abrir ───────────────────────────────────────────────

    /// <summary>
    /// Apertura de sesión de caja.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: crea SesionCajaMirror local + encola en OperacionPendiente.
    /// </summary>
    [HttpPost("abrir")]
    public async Task<IActionResult> Abrir(
        [FromBody] AbrirSesionCajaRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/caja/abrir", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(ProxyHelper.Unwrap<SesionCajaResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Caja] Abrir offline — caja={Caja}", request.CajaId);

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

        var idLocal = -Random.Shared.Next(1, 1000000);
        int.TryParse(UserId, out int usuarioIdInt);

        var sesion = new SesionCajaMirror
        {
            IdLocal       = idLocal,
            CajaId        = request.CajaId,
            NombreCaja    = request.CajaId.ToString(),
            UsuarioId     = usuarioIdInt,
            NombreUsuario = UserName,
            MontoApertura = request.MontoApertura,
            Estado        = "Abierta",
            FechaApertura = DateTimeOffset.UtcNow,
            EstadoSync    = "Pendiente"
        };
        _db.SesionesCajaMirror.Add(sesion);

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Caja",
            TipoOperacion   = "AbrirSesion",
            EndpointCore    = "/api/v1/caja/abrir",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = idLocal.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Caja] Sesión offline abierta — IdLocal={Id}", idLocal);

        return Ok(new SesionCajaResponse(
            Id: idLocal,
            CajaId: request.CajaId,
            NombreCaja: request.CajaId.ToString(),
            UsuarioId: usuarioIdInt,
            NombreUsuario: UserName,
            FechaApertura: sesion.FechaApertura.UtcDateTime,
            FechaCierre: null,
            MontoApertura: request.MontoApertura,
            MontoCierre: null,
            MontoSistema: null,
            Diferencia: null,
            Estado: "Abierta",
            TotalVentas: 0,
            TotalVendido: 0
        ));
    }

    // ── POST /api/v1/caja/cerrar ──────────────────────────────────────────────

    /// <summary>
    /// Cierre de sesión de caja.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: actualiza SesionCajaMirror + encola cierre en OperacionPendiente.
    /// </summary>
    [HttpPost("cerrar")]
    public async Task<IActionResult> Cerrar(
        [FromBody] CerrarSesionCajaRequest request, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/caja/cerrar", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(ProxyHelper.Unwrap<SesionCajaResponse>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // ── MODO OFFLINE ───────────────────────────────────────────────────────
        _logger.LogWarning("[Caja] Cerrar offline — sesión={Ses}", request.SesionId);

        var sesion = await _db.SesionesCajaMirror
            .FirstOrDefaultAsync(s => s.IdLocal == request.SesionId && s.Estado == "Abierta", ct);

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
        sesion.MontoCierre   = request.MontoCierre;
        sesion.FechaCierre   = DateTimeOffset.UtcNow;
        sesion.Observaciones = request.Observaciones ?? "Cierre registrado en modo offline";

        _db.OperacionesPendientes.Add(new OperacionPendiente
        {
            TipoEntidad     = "Caja",
            TipoOperacion   = "CerrarSesion",
            EndpointCore    = "/api/v1/caja/cerrar",
            MetodoHttp      = "POST",
            PayloadJson     = JsonSerializer.Serialize(request),
            IdLocalTemporal = request.SesionId.ToString(),
            UsuarioId       = UserId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Ok(new SesionCajaResponse(
            Id: sesion.IdLocal,
            CajaId: sesion.CajaId,
            NombreCaja: sesion.NombreCaja ?? "",
            UsuarioId: sesion.UsuarioId,
            NombreUsuario: sesion.NombreUsuario ?? "",
            FechaApertura: sesion.FechaApertura.UtcDateTime,
            FechaCierre: sesion.FechaCierre?.UtcDateTime,
            MontoApertura: sesion.MontoApertura,
            MontoCierre: request.MontoCierre,
            MontoSistema: sesion.MontoApertura,
            Diferencia: request.MontoCierre - sesion.MontoApertura,
            Estado: "Cerrada",
            TotalVentas: 0,
            TotalVendido: 0
        ));
    }
}
