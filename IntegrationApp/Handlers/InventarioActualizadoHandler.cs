using IntegrationApp.Data;
using Microsoft.EntityFrameworkCore;
using NServiceBus;
using SharedContracts.Events;

namespace IntegrationApp.Handlers;

/// <summary>
/// Recibe InventarioActualizadoEvent del Core (vía RabbitMQ) y actualiza
/// el stock del ProductoMirror en PostgreSQL.
///
/// CORRECCIONES:
/// — Usa SharedContracts.Events.InventarioActualizadoEvent (mismo namespace que Core)
///   para que NServiceBus resuelva el exchange correcto en RabbitMQ.
/// — Usa message.StockNuevo (propiedad definida en SharedContracts) en lugar de
///   StockActual (que era la propiedad del contrato local duplicado).
/// — Si el producto no existe en el mirror (se creó en Core después del último sync),
///   se registra una advertencia; el MirrorSyncService lo incorporará en el
///   próximo ciclo de polling.
/// </summary>
public class InventarioActualizadoHandler : IHandleMessages<InventarioActualizadoEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventarioActualizadoHandler> _logger;

    public InventarioActualizadoHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<InventarioActualizadoHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Handle(InventarioActualizadoEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation(
            "[Inventario] ProductoId={Id} | Stock: {Anterior} → {Nuevo} | Motivo: {Motivo}",
            message.ProductoId, message.StockAnterior, message.StockNuevo, message.Motivo);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var producto = await db.ProductosMirror.FindAsync(message.ProductoId);

        if (producto is not null)
        {
            producto.Stock = message.StockNuevo;
            producto.UltimaSync = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogDebug("[Inventario] Mirror actualizado: ProductoId={Id}, StockNuevo={Stock}",
                message.ProductoId, message.StockNuevo);
        }
        else
        {
            // El producto no está en el mirror aún. El MirrorSyncService lo incorporará
            // en el próximo ciclo de polling. No es un error crítico.
            _logger.LogWarning(
                "[Inventario] ProductoId={Id} no encontrado en mirror. " +
                "Se incorporará en el próximo ciclo de MirrorSync.",
                message.ProductoId);
        }
    }
}
