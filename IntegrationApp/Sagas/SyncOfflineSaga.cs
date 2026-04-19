using IntegrationApp.Contracts.Requests.Ventas;
using IntegrationApp.Data;
using IntegrationApp.Domain.Entities;
using IntegrationApp.Messages.Commands;
using IntegrationApp.Messages.Events;
using IntegrationApp.Services;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using System.Text.Json;

namespace IntegrationApp.Sagas;

/// <summary>
/// Saga de sincronización offline. Orquesta el proceso de aplicar ventas
/// pendientes al Core cuando se restaura la conexión.
/// </summary>
public class SyncOfflineSaga : Saga<SyncOfflineSagaData>,
    IAmStartedByMessages<VentaRealizadaOfflineMessage>,
    IHandleMessages<VentaAplicadaEnCoreEvent>,
    IHandleTimeouts<RetryTimeout>
{
    private readonly ICircuitBreakerStateService _cbState;
    private readonly ICoreApiClient _coreApiClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SyncOfflineSaga> _logger;

    private const int MaxIntentos = 3;

    public SyncOfflineSaga(
        ICircuitBreakerStateService cbState,
        ICoreApiClient coreApiClient,
        IServiceScopeFactory scopeFactory,
        ILogger<SyncOfflineSaga> logger)
    {
        _cbState = cbState;
        _coreApiClient = coreApiClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SyncOfflineSagaData> mapper)
    {
        mapper.MapSaga(s => s.IdTransaccionLocal)
              .ToMessage<VentaRealizadaOfflineMessage>(m => m.IdTransaccionLocal)
              .ToMessage<VentaAplicadaEnCoreEvent>(m => m.IdTransaccionLocal);
    }

    public async Task Handle(VentaRealizadaOfflineMessage msg, IMessageHandlerContext ctx)
    {
        _logger.LogInformation("[Saga] Recibida VentaRealizadaOfflineMessage {Id}", msg.IdTransaccionLocal);

        // Persistir en la saga data
        Data.IdTransaccionLocal = msg.IdTransaccionLocal;
        Data.SucursalId = msg.IdSucursal;
        Data.CajeroId = msg.IdCajero;
        Data.ClienteId = msg.ClienteId;
        Data.MetodoPago = msg.MetodoPago;
        Data.MontoTotal = msg.MontoTotal;
        Data.MontoRecibido = msg.MontoRecibido;
        Data.FechaLocal = msg.FechaLocal;
        Data.LineasJson = JsonSerializer.Serialize(msg.Lineas);

        // Persistir en BD local
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        // Verificar idempotencia
        var yaExiste = await db.IdempotencyLogs
            .AnyAsync(x => x.IdTransaccionLocal == msg.IdTransaccionLocal);

        if (yaExiste)
        {
            _logger.LogWarning("[Saga] Transacción {Id} ya fue procesada (duplicado)", msg.IdTransaccionLocal);
            MarkAsComplete();
            return;
        }

        // Guardar venta offline pendiente
        var ventaOffline = new VentaOfflinePendiente
        {
            IdTransaccionLocal = msg.IdTransaccionLocal,
            CajeroId = msg.IdCajero,
            SucursalId = msg.IdSucursal,
            ClienteId = msg.ClienteId,
            MetodoPago = msg.MetodoPago,
            MontoTotal = msg.MontoTotal,
            MontoRecibido = msg.MontoRecibido,
            LineasJson = Data.LineasJson,
            FechaLocal = msg.FechaLocal,
            Estado = "Pendiente"
        };

        db.VentasOfflinePendientes.Add(ventaOffline);
        await db.SaveChangesAsync();

        await IntentatSincronizar(ctx);
    }

    private async Task IntentatSincronizar(IMessageHandlerContext ctx)
    {
        if (!_cbState.CoreAvailable)
        {
            _logger.LogInformation("[Saga] Core no disponible, programando retry en 5 min para {Id}", Data.IdTransaccionLocal);
            await RequestTimeout<RetryTimeout>(ctx, TimeSpan.FromMinutes(5));
            return;
        }

        Data.Intentos++;
        try
        {
            var request = new CrearVentaRequest
            {
                ClienteId = Data.ClienteId,
                CajeroId = Data.CajeroId,
                SucursalId = Data.SucursalId,
                MetodoPago = Data.MetodoPago,
                MontoRecibido = Data.MontoRecibido,
                Lineas = JsonSerializer.Deserialize<List<LineaVentaDto>>(Data.LineasJson) ?? []
            };

            var response = await _coreApiClient.PostAsync(
                "/api/v1/ventas",
                request,
                idempotencyKey: Data.IdTransaccionLocal.ToString());

            if (response.IsSuccessStatusCode)
            {
                await ctx.Publish(new VentaSincronizadaEvent
                {
                    IdTransaccionLocal = Data.IdTransaccionLocal,
                    FacturaIdCore = Guid.Empty, // Se poblará desde VentaAplicadaEnCoreEvent
                    NumeroFactura = "PENDIENTE",
                    Resultado = "Sincronizada",
                    SincronizadaEn = DateTimeOffset.UtcNow
                });

                _logger.LogInformation("[Saga] Venta {Id} sincronizada exitosamente", Data.IdTransaccionLocal);
                MarkAsComplete();
            }
            else
            {
                await ManejarFallo(ctx, $"HTTP {(int)response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            await ManejarFallo(ctx, ex.Message);
        }
    }

    private async Task ManejarFallo(IMessageHandlerContext ctx, string razon)
    {
        _logger.LogWarning("[Saga] Intento {N}/{Max} falló: {Razon}", Data.Intentos, MaxIntentos, razon);

        if (Data.Intentos >= MaxIntentos)
        {
            _logger.LogError("[Saga] Máximos intentos alcanzados para {Id}. Enviando a dead-letter.", Data.IdTransaccionLocal);
            await ctx.Publish(new VentaSincronizadaEvent
            {
                IdTransaccionLocal = Data.IdTransaccionLocal,
                FacturaIdCore = Guid.Empty,
                NumeroFactura = string.Empty,
                Resultado = "RechazadaCore",
                MotivoRechazo = razon,
                SincronizadaEn = DateTimeOffset.UtcNow
            });
            MarkAsComplete();
        }
        else
        {
            // Backoff exponencial: 5, 10, 20 minutos
            var delay = TimeSpan.FromMinutes(5 * Math.Pow(2, Data.Intentos - 1));
            await RequestTimeout<RetryTimeout>(ctx, delay);
        }
    }

    public async Task Handle(VentaAplicadaEnCoreEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("[Saga] Venta {Id} confirmada por Core con factura {Factura}",
            message.IdTransaccionLocal, message.NumeroFactura);
        MarkAsComplete();
        await Task.CompletedTask;
    }

    public async Task Timeout(RetryTimeout state, IMessageHandlerContext context)
    {
        _logger.LogInformation("[Saga] Timeout disparado para {Id} — reintentando sincronización", Data.IdTransaccionLocal);
        await IntentatSincronizar(context);
    }
}

// Timeout marker
public class RetryTimeout { }

// Evento que el Core envía para confirmar aplicación (P2 → P3)
public record VentaAplicadaEnCoreEvent : NServiceBus.IEvent
{
    public Guid IdTransaccionLocal { get; init; }
    public Guid FacturaIdCore { get; init; }
    public string NumeroFactura { get; init; } = string.Empty;
}
