using NServiceBus;

namespace IntegrationApp.Messages.Events;

/// <summary>P3 → P1: Confirma que la venta offline fue aplicada en el Core.</summary>
public record VentaSincronizadaEvent : IEvent
{
    public Guid IdTransaccionLocal { get; init; }
    public Guid FacturaIdCore { get; init; }
    public string NumeroFactura { get; init; } = string.Empty;
    public string Resultado { get; init; } = string.Empty;       // "Sincronizada" | "RechazadaCore"
    public string? MotivoRechazo { get; init; }
    public DateTimeOffset SincronizadaEn { get; init; }
}

/// <summary>P3 → P1: Todas las transacciones offline del lote están sincronizadas.</summary>
public record ReconciliacionCompletadaEvent : IEvent
{
    public string SucursalId { get; init; } = string.Empty;
    public int TotalTransacciones { get; init; }
    public int Aplicadas { get; init; }
    public int Rechazadas { get; init; }
    public DateTimeOffset CompletadaEn { get; init; }
}

/// <summary>P2 → P3: El Core notifica cambios de stock. P3 actualiza ProductoMirror.</summary>
public record InventarioActualizadoEvent : IEvent
{
    public int ProductoId { get; init; }
    public int StockNuevo { get; init; }
    public int StockAnterior { get; init; }
    public string Motivo { get; init; } = string.Empty;   // "Venta" | "Ajuste" | "Compra" | "Devolucion"
    public DateTimeOffset Fecha { get; init; }
}

/// <summary>P2 → P3 → P4: El Core notifica cambios de estado de órdenes.</summary>
public record OrdenCambioEstadoEvent : IEvent
{
    public Guid OrdenId { get; init; }
    public string EstadoNuevo { get; init; } = string.Empty;
    public string EstadoAnterior { get; init; } = string.Empty;
    public string? Nota { get; init; }
    public DateTimeOffset Fecha { get; init; }
}
