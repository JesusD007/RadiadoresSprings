using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Messages.Events;
using IntegrationApp.Services;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using SharedContracts.Commands;
using SharedContracts.Events;
using System.Text.Json;

namespace IntegrationApp.Sagas;

/// <summary>
/// Orquesta la sincronización de ventas offline al Core.
///
/// FLUJO NORMAL (Core disponible):
///   1. Recibe VentaRealizadaOfflineMessage (de Caja vía RabbitMQ)
///   2. Persiste VentaOfflinePendiente + IdempotencyLog en PostgreSQL
///   3. Obtiene JWT del Core (CoreTokenService)
///   4. POST /api/v1/ventas con Idempotency-Key
///   5a. 2xx → actualiza Estado "Sincronizada", publica VentaSincronizadaEvent → MarkAsComplete()
///   5b. Error → backoff exponencial (5 / 10 / 20 min), máx 3 reintentos → dead-letter
///
/// FLUJO OFFLINE (Core no disponible):
///   1-2. Ídem
///   3. CircuitBreaker abierto → programa timeout 5 min
///   4. Al expirar el timeout reintenta desde paso 3
///
/// CONFIRMACIÓN POR BUS (opcional, complementario):
///   Si Core publica VentaAplicadaEnCoreEvent vía RabbitMQ (por ejemplo al
///   procesar el idempotency key en VentasController), el saga actualiza
///   el IdempotencyLog con los datos reales de factura aunque ya haya completado.
/// </summary>
public class SyncOfflineSaga : Saga<SyncOfflineSagaData>,
    IAmStartedByMessages<VentaRealizadaOfflineMessage>,
    IHandleMessages<VentaAplicadaEnCoreEvent>,
    IHandleTimeouts<RetryTimeout>
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ICoreApiClient _coreApiClient;
    private readonly ICoreTokenService _coreTokenService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncOfflineSaga> _logger;

    private const int MaxIntentos = 3;

    public SyncOfflineSaga(
        ICircuitBreakerStateService cbState,
        ICoreApiClient coreApiClient,
        ICoreTokenService coreTokenService,
        IServiceScopeFactory scopeFactory,
        ILogger<SyncOfflineSaga> logger)
    {
        _cbState = cbState;
        _coreApiClient = coreApiClient;
        _coreTokenService = coreTokenService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SyncOfflineSagaData> mapper)
    {
        mapper.MapSaga(s => s.IdTransaccionLocal)
              .ToMessage<VentaRealizadaOfflineMessage>(m => m.IdTransaccionLocal)
              .ToMessage<VentaAplicadaEnCoreEvent>(m => m.IdTransaccionLocal);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Entrada: nueva venta offline desde la Caja
    // ─────────────────────────────────────────────────────────────────────────
    public async Task Handle(VentaRealizadaOfflineMessage msg, IMessageHandlerContext ctx)
    {
        _logger.LogInformation("[Saga] Recibida VentaRealizadaOfflineMessage {Id}", msg.IdTransaccionLocal);

        // Copiar payload al saga data (persiste en NServiceBus SQL Persistence)
        Data.IdTransaccionLocal = msg.IdTransaccionLocal;
        Data.SucursalId = msg.IdSucursal;
        Data.CajeroId = msg.IdCajero;
        Data.CajaId = msg.CajaId;
        Data.SesionCajaId = msg.SesionCajaId;
        Data.ClienteId = msg.ClienteId;
        Data.MetodoPago = msg.MetodoPago;
        Data.MontoTotal = msg.MontoTotal;
        Data.MontoRecibido = msg.MontoRecibido;
        Data.Descuento = msg.Descuento;
        Data.Observaciones = msg.Observaciones;
        Data.FechaLocal = msg.FechaLocal;
        Data.LineasJson = JsonSerializer.Serialize(msg.Lineas);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        // ── IDEMPOTENCIA: verificar IdempotencyLog (escenario normal de reentrega) ──
        var yaExiste = await db.IdempotencyLogs
            .AnyAsync(x => x.IdTransaccionLocal == msg.IdTransaccionLocal, ctx.CancellationToken);

        if (yaExiste)
        {
            _logger.LogWarning("[Saga] Transacción {Id} ya fue procesada (duplicado) — descartando", msg.IdTransaccionLocal);
            MarkAsComplete();
            return;
        }

        // ── IDEMPOTENCIA: verificar VentaOfflinePendiente ────────────────────────
        // Cubre el escenario donde la venta fue insertada por una versión anterior del
        // código (sin IdempotencyLog) o tras un fallo parcial post-SaveChangesAsync.
        // En ese caso el registro ya está en BD: saltamos la inserción y vamos
        // directo a intentar la sincronización con Core.
        var ventaYaExiste = await db.VentasOfflinePendientes
            .AnyAsync(v => v.IdTransaccionLocal == msg.IdTransaccionLocal, ctx.CancellationToken);

        if (!ventaYaExiste)
        {
            // Escribir el log de idempotencia
            db.IdempotencyLogs.Add(new IdempotencyLog
            {
                IdTransaccionLocal = msg.IdTransaccionLocal,
                Estado = "Recibida",
                FechaEnvio = DateTimeOffset.UtcNow
            });

            // Guardar venta offline pendiente en la BD local
            db.VentasOfflinePendientes.Add(new VentaOfflinePendiente
            {
                IdTransaccionLocal = msg.IdTransaccionLocal,
                CajeroId = msg.IdCajero,
                SucursalId = msg.IdSucursal,
                CajaId = msg.CajaId,
                SesionCajaId = msg.SesionCajaId,
                ClienteId = msg.ClienteId,
                MetodoPago = msg.MetodoPago,
                MontoTotal = msg.MontoTotal,
                MontoRecibido = msg.MontoRecibido,
                Descuento = msg.Descuento,
                Observaciones = msg.Observaciones,
                LineasJson = Data.LineasJson,
                FechaLocal = msg.FechaLocal,
                Estado = "Pendiente"
            });

            await db.SaveChangesAsync(ctx.CancellationToken);
        }
        else
        {
            _logger.LogWarning(
                "[Saga] VentaOfflinePendiente {Id} ya existe en BD (sin IdempotencyLog). " +
                "Posible fallo post-escritura en versión anterior. Continuando hacia sincronización.",
                msg.IdTransaccionLocal);

            // Asegurar que el IdempotencyLog quede registrado para futuras reentregas
            db.IdempotencyLogs.Add(new IdempotencyLog
            {
                IdTransaccionLocal = msg.IdTransaccionLocal,
                Estado = "Recibida",
                FechaEnvio = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ctx.CancellationToken);
        }

        await IntentarSincronizar(ctx);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Intento de sincronización (llamado en Handle y en Timeout)
    // ─────────────────────────────────────────────────────────────────────────
    private async Task IntentarSincronizar(IMessageHandlerContext ctx)
    {
        if (!_cbState.CoreAvailable)
        {
            _logger.LogInformation("[Saga] Core no disponible. Retry en 5 min para {Id}", Data.IdTransaccionLocal);
            await RequestTimeout<RetryTimeout>(ctx, TimeSpan.FromMinutes(5));
            return;
        }

        Data.Intentos++;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        try
        {
            // ── BUG #6/#7 CORREGIDO: obtener JWT antes de llamar al Core ─────
            var token = await _coreTokenService.GetTokenAsync(ctx.CancellationToken);

            // Reconstruir el payload de la venta desde los datos del saga.
            // LineasJson fue serializado desde LineaVentaItem (SharedContracts),
            // así que deserializamos al mismo tipo.
            var lineas = JsonSerializer.Deserialize<List<LineaVentaItem>>(Data.LineasJson) ?? [];
            var request = new
            {
                clienteId    = Data.ClienteId,
                cajeroId     = Data.CajeroId,
                sucursalId   = Data.SucursalId,
                cajaId       = Data.CajaId,
                sesionCajaId = Data.SesionCajaId,
                metodoPago   = Data.MetodoPago,
                montoRecibido = Data.MontoRecibido,
                descuento    = Data.Descuento,
                observaciones = Data.Observaciones,
                lineas       = lineas.Select(l => new
                {
                    productoId     = l.ProductoId,
                    cantidad       = l.Cantidad,
                    precioUnitario = l.PrecioUnitario
                }).ToList()
            };

            var response = await _coreApiClient.PostAsync(
                "/api/v1/ventas",
                request,
                bearerToken: token,
                idempotencyKey: Data.IdTransaccionLocal.ToString(),
                ct: ctx.CancellationToken);

            // ── BUG #5 CORREGIDO: actualizar estado en BD ────────────────────
            await ActualizarVentaPendienteAsync(db, Data.Intentos, exito: response.IsSuccessStatusCode);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Saga] Venta {Id} sincronizada al Core (intento {N})", Data.IdTransaccionLocal, Data.Intentos);

                // Actualizar IdempotencyLog como "Enviada" (confirmación definitiva llegará
                // vía VentaAplicadaEnCoreEvent si Core lo publica, o queda en "Enviada")
                await ActualizarIdempotencyLogAsync(db, "Enviada", motivoRechazo: null);

                // Notificar a la Caja POS que la venta fue sincronizada
                await ctx.Publish(new VentaSincronizadaEvent
                {
                    IdTransaccionLocal = Data.IdTransaccionLocal,
                    VentaIdCore = 0,          // Se actualiza al recibir VentaAplicadaEnCoreEvent
                    NumeroFactura = string.Empty, // Ídem
                    Resultado = "Sincronizada",
                    SincronizadaEn = DateTimeOffset.UtcNow
                });

                MarkAsComplete();
            }
            else
            {
                await ManejarFallo(ctx, db, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await ManejarFallo(ctx, db, ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Manejo de fallo con backoff exponencial
    // ─────────────────────────────────────────────────────────────────────────
    private async Task ManejarFallo(IMessageHandlerContext ctx, IntegrationDbContext db, string razon)
    {
        _logger.LogWarning("[Saga] Intento {N}/{Max} falló para {Id}: {Razon}",
            Data.Intentos, MaxIntentos, Data.IdTransaccionLocal, razon);

        if (Data.Intentos >= MaxIntentos)
        {
            _logger.LogError("[Saga] Máx. intentos alcanzados para {Id}. Enviando a dead-letter.", Data.IdTransaccionLocal);

            // ── BUG #5 CORREGIDO: marcar como rechazada en BD ────────────────
            await ActualizarVentaPendienteEstadoAsync(db, "RechazadaCore");
            await ActualizarIdempotencyLogAsync(db, "Rechazada", motivoRechazo: razon);

            await ctx.Publish(new VentaSincronizadaEvent
            {
                IdTransaccionLocal = Data.IdTransaccionLocal,
                VentaIdCore = 0,
                NumeroFactura = string.Empty,
                Resultado = "RechazadaCore",
                MotivoRechazo = razon,
                SincronizadaEn = DateTimeOffset.UtcNow
            });

            MarkAsComplete();
        }
        else
        {
            // Backoff exponencial: 5 min → 10 min → 20 min
            var delay = TimeSpan.FromMinutes(5 * Math.Pow(2, Data.Intentos - 1));
            _logger.LogInformation("[Saga] Reintentando en {Min:F0} min para {Id}", delay.TotalMinutes, Data.IdTransaccionLocal);
            await RequestTimeout<RetryTimeout>(ctx, delay);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Confirmación por bus desde el Core (VentaAplicadaEnCoreEvent)
    //    Complementario: llega si Core publica el evento tras aplicar la venta.
    //    El saga puede ya estar completo; NServiceBus lo ignora silenciosamente.
    // ─────────────────────────────────────────────────────────────────────────
    public async Task Handle(VentaAplicadaEnCoreEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("[Saga] Confirmación del Core para {Id}: Factura={Factura}, Exitoso={Ok}",
            message.IdTransaccionLocal, message.NumeroFactura, message.Exitoso);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        if (message.Exitoso)
        {
            // Actualizar con los datos reales de la factura del Core
            var log = await db.IdempotencyLogs
                .FirstOrDefaultAsync(x => x.IdTransaccionLocal == message.IdTransaccionLocal, context.CancellationToken);

            if (log is not null)
            {
                log.Estado = "Aplicada";
                log.FechaConfirmacion = message.Timestamp;
                await db.SaveChangesAsync(context.CancellationToken);
            }
        }

        // Si el saga llegó hasta aquí desde un timeout ya completado,
        // NServiceBus maneja el caso de saga no encontrada sin crash.
        MarkAsComplete();
        await Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Timeout: reintento programado
    // ─────────────────────────────────────────────────────────────────────────
    public async Task Timeout(RetryTimeout state, IMessageHandlerContext context)
    {
        _logger.LogInformation("[Saga] Timeout disparado para {Id} — reintentando", Data.IdTransaccionLocal);
        await IntentarSincronizar(context);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers de BD
    // ─────────────────────────────────────────────────────────────────────────

    private async Task ActualizarVentaPendienteAsync(IntegrationDbContext db, int intentos, bool exito)
    {
        var venta = await db.VentasOfflinePendientes
            .FirstOrDefaultAsync(v => v.IdTransaccionLocal == Data.IdTransaccionLocal);

        if (venta is null) return;

        venta.IntentosSync = intentos;
        venta.UltimoIntento = DateTimeOffset.UtcNow;

        if (exito)
            venta.Estado = "Sincronizada";

        await db.SaveChangesAsync();
    }

    private async Task ActualizarVentaPendienteEstadoAsync(IntegrationDbContext db, string estado)
    {
        var venta = await db.VentasOfflinePendientes
            .FirstOrDefaultAsync(v => v.IdTransaccionLocal == Data.IdTransaccionLocal);

        if (venta is null) return;

        venta.Estado = estado;
        venta.UltimoIntento = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
    }

    private async Task ActualizarIdempotencyLogAsync(IntegrationDbContext db, string estado, string? motivoRechazo)
    {
        var log = await db.IdempotencyLogs
            .FirstOrDefaultAsync(x => x.IdTransaccionLocal == Data.IdTransaccionLocal);

        if (log is null) return;

        log.Estado = estado;
        log.MotivoRechazo = motivoRechazo;

        if (estado is "Aplicada" or "Enviada")
            log.FechaConfirmacion = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();
    }
}

/// <summary>Marcador para el timeout de reintento del saga.</summary>
public class RetryTimeout { }
