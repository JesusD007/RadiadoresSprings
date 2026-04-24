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
[Route("api/v1/cuentas-cobrar")]
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
                ? $"/api/v1/cuentas-cobrar/{coreId}"
                : $"/api/v1/cuentas-cobrar/{clienteId}";

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
}
