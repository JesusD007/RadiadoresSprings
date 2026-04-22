using IntegrationApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using NServiceBus;
using SharedContracts.Events;

namespace IntegrationApp.Handlers;

/// <summary>
/// Recibe OrdenCambioEstadoEvent del Core (vía RabbitMQ) y lo reenvía
/// al Website (P4) en tiempo real mediante SignalR.
///
/// CORRECCIONES:
/// — Usa SharedContracts.Events.OrdenCambioEstadoEvent (mismo namespace que Core)
///   para que NServiceBus resuelva el exchange correcto en RabbitMQ.
/// — Usa message.Timestamp y message.Nota (propiedades definidas en SharedContracts)
///   en lugar de Fecha/Nota del contrato local que tenía tipos incompatibles.
/// </summary>
public class OrdenCambioEstadoHandler : IHandleMessages<OrdenCambioEstadoEvent>
{
    private readonly IHubContext<NotificacionesHub> _hubContext;
    private readonly ILogger<OrdenCambioEstadoHandler> _logger;

    public OrdenCambioEstadoHandler(
        IHubContext<NotificacionesHub> hubContext,
        ILogger<OrdenCambioEstadoHandler> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(OrdenCambioEstadoEvent message, IMessageHandlerContext context)
    {
        _logger.LogInformation("[OrdenEstado] Orden {Id}: {Anterior} → {Nuevo}",
            message.OrdenId, message.EstadoAnterior, message.EstadoNuevo);

        // Notificar en tiempo real al grupo de la orden vía SignalR
        await _hubContext.Clients
            .Group($"orden-{message.OrdenId}")
            .SendAsync("EstadoOrdenActualizado", new
            {
                message.OrdenId,
                message.EstadoNuevo,
                message.EstadoAnterior,
                message.Nota,
                Timestamp = message.Timestamp
            });
    }
}
