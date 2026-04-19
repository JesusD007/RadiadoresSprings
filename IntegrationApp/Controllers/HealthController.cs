using IntegrationApp.Contracts.Responses.Health;
using IntegrationApp.Data;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly IConfiguration _config;

    public HealthController(ICircuitBreakerStateService cbState, IntegrationDbContext db, IConfiguration config)
    {
        _cbState = cbState;
        _db = db;
        _config = config;
    }

    /// <summary>Estado de Integración para circuit breaker de Polly y monitoreo general.</summary>
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> GetHealth(CancellationToken ct)
    {
        var checks = new Dictionary<string, string>();
        bool dbOk = false;
        int pendientes = 0;

        // Verificar BD local
        try
        {
            await _db.Database.CanConnectAsync(ct);
            dbOk = true;
            checks["db"] = "Healthy";
            pendientes = await _db.VentasOfflinePendientes
                .CountAsync(v => v.Estado == "Pendiente" || v.Estado == "EnCola", ct);
        }
        catch
        {
            checks["db"] = "Unhealthy";
        }

        var coreOk = _cbState.CoreAvailable;
        checks["core"] = coreOk ? "Healthy" : "Degraded";
        checks["nsb"] = "Healthy"; // NServiceBus Learning transport siempre activo

        var ultimaSync = _cbState.UltimaSync ?? DateTimeOffset.MinValue;
        var mirrorActualizado = ultimaSync > DateTimeOffset.UtcNow.AddMinutes(
            -_config.GetValue<int>("Mirror:SyncIntervalMinutes", 5) * 2);

        var status = (!dbOk)
            ? "Unhealthy"
            : (!coreOk)
                ? "Degraded"
                : "Healthy";

        return Ok(new HealthResponse
        {
            Status = status,
            CoreDisponible = coreOk,
            MirrorActualizado = mirrorActualizado,
            ColasPendientes = pendientes,
            UltimaSync = ultimaSync,
            Checks = checks
        });
    }
}
