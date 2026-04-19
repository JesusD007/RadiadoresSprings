using IntegrationApp.Hubs;
using IntegrationApp.Messages.Events;
using Microsoft.AspNetCore.SignalR;
using NServiceBus;

namespace IntegrationApp.Handlers;

/// <summary>
/// Recibe OrdenCambioEstadoEvent del Core y lo reenvía al Website (P4) vía SignalR.
/// </summary>
public class OrdenCambioEstadoHandler : IHandleMessages<OrdenCambioEstadoEvent>
{
    private readonly IHubContext<NotificacionesHub> _hubContext;
    private readonly ILogger<OrdenCambioEstadoHandler> _logger;

    public OrdenCambioEstadoHandler(IHubContext<NotificacionesHub> hubContext, ILogger<OrdenCambioEstadoHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(OrdenCambioEstadoEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("[OrdenEstado] Orden {Id}: {Anterior}→{Nuevo}", message.OrdenId, message.EstadoAnterior, message.EstadoNuevo);

        // Notificar al grupo de la orden vía SignalR
        await _hubContext.Clients
            .Group($"orden-{message.OrdenId}")
            .SendAsync("EstadoOrdenActualizado", new
            {
                message.OrdenId,
                message.EstadoNuevo,
                message.EstadoAnterior,
                message.Nota,
                message.Fecha
            });
    }
}
