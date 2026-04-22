using NServiceBus;

namespace SharedContracts.Events;

/// <summary>
/// Core (P2) → Integration (P3) → Website (P4): notifica un cambio de estado
/// de una orden. Integration lo reenvía en tiempo real vía SignalR.
/// </summary>
public record OrdenCambioEstadoEvent : IEvent
{
    /// <summary>PK de la orden en SQL Server (int auto-increment).</summary>
    public int OrdenId { get; init; }

    /// <summary>Estado anterior de la orden.</summary>
    public string EstadoAnterior { get; init; } = string.Empty;

    /// <summary>Estado nuevo de la orden.</summary>
    public string EstadoNuevo { get; init; } = string.Empty;

    /// <summary>Nota u observación opcional al cambiar el estado.</summary>
    public string? Nota { get; init; }

    /// <summary>Momento UTC del cambio de estado en el Core.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
