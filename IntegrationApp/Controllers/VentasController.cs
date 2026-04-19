using IntegrationApp.Contracts.Requests.Ventas;
using IntegrationApp.Contracts.Responses.Ventas;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Messages.Commands;
using IntegrationApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using System.Text.Json;

namespace IntegrationApp.Controllers;

[ApiController]
[Route("api/v1/ventas")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly ICoreApiClient _core;
    private readonly ICircuitBreakerStateService _cbState;
    private readonly IntegrationDbContext _db;
    private readonly IMessageSession _bus;
    private readonly ILogger<VentasController> _logger;
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public VentasController(ICoreApiClient core, ICircuitBreakerStateService cbState,
        IntegrationDbContext db, IMessageSession bus, ILogger<VentasController> logger)
    {
        _core = core; _cbState = cbState; _db = db; _bus = bus; _logger = logger;
    }

    /// <summary>
    /// [CRÍTICO] Crear venta/factura. 
    /// Si Core disponible → proxy. Si offline → persiste localmente y encola.
    /// Requiere header Idempotency-Key.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VentaResponse>> CrearVenta(
        [FromBody] CrearVentaRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { error = "El header Idempotency-Key es requerido" });

        if (!Guid.TryParse(idempotencyKey, out var idGuid))
            return BadRequest(new { error = "Idempotency-Key debe ser un GUID válido" });

        // Verificar idempotencia — ya fue procesada?
        var yaProcessada = await _db.IdempotencyLogs
            .FirstOrDefaultAsync(x => x.IdTransaccionLocal == idGuid, ct);

        if (yaProcessada is not null)
        {
            _logger.LogInformation("[Ventas] Idempotency-Key {Id} ya procesada — retornando respuesta original", idGuid);
            return Conflict(new { error = "Idempotency-Key ya procesada", estado = yaProcessada.Estado });
        }

        var token = Request.Headers.Authorization.ToString().Replace("Bearer ", "");

        // === MODO ONLINE: proxy al Core ===
        if (_cbState.CoreAvailable)
        {
            var response = await _core.PostAsync("/api/v1/ventas", request,
                bearerToken: token, idempotencyKey: idempotencyKey, ct: ct);

            var content = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var ventaResp = JsonSerializer.Deserialize<VentaResponse>(content, _json)!;

                // Registrar idempotencia
                _db.IdempotencyLogs.Add(new IdempotencyLog
                {
                    IdTransaccionLocal = idGuid,
                    FacturaIdCore = ventaResp.FacturaId,
                    Estado = "Aplicada",
                    FechaEnvio = DateTimeOffset.UtcNow,
                    FechaConfirmacion = DateTimeOffset.UtcNow
                });
                await _db.SaveChangesAsync(ct);

                return StatusCode(201, ventaResp);
            }

            return response.StatusCode switch
            {
                System.Net.HttpStatusCode.UnprocessableEntity => UnprocessableEntity(JsonSerializer.Deserialize<object>(content, _json)),
                System.Net.HttpStatusCode.Conflict => Conflict(JsonSerializer.Deserialize<object>(content, _json)),
                _ => StatusCode((int)response.StatusCode, JsonSerializer.Deserialize<object>(content, _json))
            };
        }

        // === MODO OFFLINE: persistir localmente + encolar ===
        _logger.LogWarning("[Ventas] Core no disponible — persistiendo venta offline {Id}", idGuid);

        var ventaOffline = new VentaOfflinePendiente
        {
            IdTransaccionLocal = idGuid,
            CajeroId = request.CajeroId,
            SucursalId = request.SucursalId,
            ClienteId = request.ClienteId,
            MetodoPago = request.MetodoPago,
            MontoTotal = request.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario - (l.Descuento ?? 0)),
            MontoRecibido = request.MontoRecibido,
            LineasJson = JsonSerializer.Serialize(request.Lineas),
            FechaLocal = DateTimeOffset.UtcNow,
            Estado = "Pendiente"
        };

        _db.VentasOfflinePendientes.Add(ventaOffline);
        await _db.SaveChangesAsync(ct);

        // Publicar mensaje NServiceBus para la Saga
        await _bus.Send(new VentaRealizadaOfflineMessage
        {
            IdTransaccionLocal = idGuid,
            IdCajero = request.CajeroId,
            IdSucursal = request.SucursalId,
            ClienteId = request.ClienteId,
            MetodoPago = request.MetodoPago,
            MontoTotal = ventaOffline.MontoTotal,
            MontoRecibido = request.MontoRecibido,
            Lineas = request.Lineas,
            FechaLocal = ventaOffline.FechaLocal
        });

        return StatusCode(201, new VentaResponse
        {
            FacturaId = Guid.NewGuid(),
            NumeroFactura = $"OFFLINE-{idGuid:N[..8]}",
            Subtotal = ventaOffline.MontoTotal,
            Itbis = ventaOffline.MontoTotal * 0.18m,
            Total = ventaOffline.MontoTotal,
            Cambio = request.MontoRecibido - ventaOffline.MontoTotal,
            FechaHora = ventaOffline.FechaLocal,
            Estado = "Pendiente",
            DesdeOffline = true
        });
    }
}
