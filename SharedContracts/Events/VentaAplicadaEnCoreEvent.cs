using NServiceBus;

namespace SharedContracts.Events;

/// <summary>
/// Core (P2) → Integration (P3): confirma que una transacción offline fue
/// procesada (o rechazada) en la base de datos del Core.
/// La Saga SyncOfflineSaga escucha este evento para completar el ciclo de sync
/// y actualizar el IdempotencyLog con los datos reales de la factura.
/// </summary>
public record VentaAplicadaEnCoreEvent : IEvent
{
    /// <summary>
    /// GUID generado por la Caja (P1) al momento de la venta offline.
    /// Actúa como idempotency key a lo largo de todo el flujo.
    /// </summary>
    public Guid IdTransaccionLocal { get; init; }

    /// <summary>PK de la Venta creada en SQL Server. 0 si Exitoso = false.</summary>
    public int VentaId { get; init; }

    /// <summary>Número de factura generado por el Core (ej. "F-000042"). Vacío si Exitoso = false.</summary>
    public string NumeroFactura { get; init; } = string.Empty;

    /// <summary>Monto total aplicado.</summary>
    public decimal Total { get; init; }

    /// <summary>True si la transacción fue aceptada y persistida en el Core.</summary>
    public bool Exitoso { get; init; }

    /// <summary>Mensaje de error del Core si Exitoso = false.</summary>
    public string? Error { get; init; }

    /// <summary>Momento UTC en que el Core aplicó (o rechazó) la transacción.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
