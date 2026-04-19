using Microsoft.AspNetCore.SignalR;

namespace IntegrationApp.Hubs;

/// <summary>
/// Hub SignalR para notificaciones en tiempo real.
/// - P4 (Website) se suscribe a cambios de estado de sus órdenes.
/// - P1 (Caja) se suscribe a eventos de reconciliación offline de su sucursal.
/// </summary>
public class NotificacionesHub : Hub
{
    private readonly ILogger<NotificacionesHub> _logger;

    public NotificacionesHub(ILogger<NotificacionesHub> logger) => _logger = logger;

    /// <summary>Website (P4) se suscribe a cambios de estado de una orden específica.</summary>
    public async Task SubscribeToOrden(string ordenId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"orden-{ordenId}");
        _logger.LogDebug("[Hub] Cliente {ConnId} suscrito a orden {OrdenId}", Context.ConnectionId, ordenId);
    }

    /// <summary>Caja (P1) se suscribe a eventos de su sucursal (reconciliación, etc.).</summary>
    public async Task SubscribeToSucursal(string sucursalId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sucursal-{sucursalId}");
        _logger.LogDebug("[Hub] Cliente {ConnId} suscrito a sucursal {SucursalId}", Context.ConnectionId, sucursalId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("[Hub] Cliente desconectado: {ConnId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
