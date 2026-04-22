using NServiceBus;

namespace SharedContracts.Events;

/// <summary>
/// Core (P2) → Integration (P3): notifica un cambio de stock en un producto.
/// Integration actualiza el ProductoMirror en su BD local al recibir este evento.
/// </summary>
public record InventarioActualizadoEvent : IEvent
{
    /// <summary>PK del producto en la BD del Core (int auto-increment en SQL Server).</summary>
    public int ProductoId { get; init; }

    /// <summary>Stock después del movimiento.</summary>
    public int StockNuevo { get; init; }

    /// <summary>Stock antes del movimiento (útil para auditoría en Integration).</summary>
    public int StockAnterior { get; init; }

    /// <summary>"Venta" | "Ajuste" | "Compra" | "Devolucion"</summary>
    public string Motivo { get; init; } = string.Empty;

    /// <summary>Momento UTC del movimiento de stock en el Core.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
