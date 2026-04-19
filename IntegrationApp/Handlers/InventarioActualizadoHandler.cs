using IntegrationApp.Data;
using IntegrationApp.Messages.Events;
using Microsoft.EntityFrameworkCore;
using NServiceBus;

namespace IntegrationApp.Handlers;

/// <summary>
/// Recibe InventarioActualizadoEvent del Core y actualiza el stock en ProductoMirror.
/// </summary>
public class InventarioActualizadoHandler : IHandleMessages<InventarioActualizadoEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventarioActualizadoHandler> _logger;

    public InventarioActualizadoHandler(IServiceScopeFactory scopeFactory, ILogger<InventarioActualizadoHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Handle(InventarioActualizadoEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("[Inventario] Actualizando stock ProductoId={Id}: {Anterior}→{Nuevo} ({Motivo})",
            message.ProductoId, message.StockAnterior, message.StockNuevo, message.Motivo);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var producto = await db.ProductosMirror.FindAsync(message.ProductoId);
        if (producto is not null)
        {
            producto.Stock = message.StockNuevo;
            producto.UltimaSync = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("[Inventario] ProductoId={Id} no encontrado en mirror", message.ProductoId);
        }
    }
}
