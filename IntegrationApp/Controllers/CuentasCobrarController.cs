using IntegrationApp.Contracts.Requests.CuentasCobrar;
using IntegrationApp.Contracts.Responses.CuentasCobrar;
using IntegrationApp.Data;
using IntegrationApp.Helpers;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/cuentascobrar")]
[Authorize]
public class CuentasCobrarController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public CuentasCobrarController(
        ICoreApiClient core,
        ICircuitBreakerStateService cbState,
        IntegrationDbContext db)
    {
        _core = core;
        _cbState = cbState;
        _db = db;
    }

    private string Token => Request.Headers.Authorization.ToString().Replace("Bearer ", "");

    /// <summary>
    /// Cuentas por cobrar pendientes del cliente con abonos.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: retorna lista vacía con advertencia — no hay mirror de cuentas por cobrar
    ///          (dato financiero demasiado volátil para cachear offline de forma confiable).
    /// </summary>
    [HttpGet("{clienteId:int}")]
    public async Task<ActionResult<IReadOnlyList<CuentaPorCobrarDto>>> GetCuentasCobrar(
        int clienteId, CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            // Resolver CoreId si el clienteId es un LocalId del mirror
            var mirror = await _db.ClientesMirror
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.LocalId == clienteId, ct);

            string endpoint = mirror?.CoreId is int coreId
                ? $"/api/v1/cuentascobrar/{coreId}"
                : $"/api/v1/cuentascobrar/{clienteId}";

            var response = await _core.GetAsync(endpoint, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(ProxyHelper.Unwrap<IReadOnlyList<CuentaPorCobrarDto>>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // Offline: no hay mirror de cuentas por cobrar — devolver lista vacía con advertencia
        Response.Headers["X-Offline-Mode"] = "true";
        Response.Headers["X-Offline-Warning"] =
            "Cuentas por cobrar no disponibles en modo offline";

        return Ok(Array.Empty<CuentaPorCobrarDto>());
    }

    /// <summary>
    /// Registrar abono a cuenta por cobrar.
    /// ONLINE:  proxy al Core.
    /// OFFLINE: encola en OperacionPendiente.
    /// </summary>
    [HttpPost("{clienteId:int}/abono")]
    public async Task<IActionResult> RegistrarAbono(
        int clienteId,
        [FromBody] RegistrarAbonoRequest request,
        CancellationToken ct)
    {
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync(
                $"/api/v1/cuentascobrar/{clienteId}/abono", request, bearerToken: Token, ct: ct);
            var content = await response.Content.ReadAsStringAsync(ct);

            return response.IsSuccessStatusCode
                ? Ok(JsonSerializer.Deserialize<object>(content, _json))
                : StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json));
        }

        // Offline: encolar abono para sincronizar cuando Core vuelva
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";

        _db.OperacionesPendientes.Add(new IntegrationApp.Domain.Entities.OperacionPendiente
        {
            TipoEntidad     = "CuentaCobrar",
            TipoOperacion   = "Abono",
            EndpointCore    = $"/api/v1/cuentascobrar/{clienteId}/abono",
            MetodoHttp      = "POST",
            PayloadJson     = System.Text.Json.JsonSerializer.Serialize(request),
            IdLocalTemporal = clienteId.ToString(),
            UsuarioId       = userId,
            FechaCreacion   = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        return Accepted(new
        {
            offline = true,
            message = "Abono registrado localmente. Se aplicará cuando el sistema central esté disponible.",
            monto   = request.Monto
        });
    }
}
