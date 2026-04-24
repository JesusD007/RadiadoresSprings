using NServiceBus;

namespace IntegrationApp.Messages.Events;

// ─────────────────────────────────────────────────────────────────────────────
// NOTA: Los eventos cross-service publicados por Core y consumidos aquí
// (InventarioActualizadoEvent, OrdenCambioEstadoEvent, VentaAplicadaEnCoreEvent)
// están en SharedContracts.Events — NO en este archivo.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// P3 → P1: Confirma al POS que la venta offline fue procesada (o rechazada)
/// en el Core. El POS puede liberar la transacción local al recibirla.
/// </summary>
public record VentaSincronizadaEvent : IEvent
{
    public Guid IdTransaccionLocal { get; init; }

    /// <summary>PK de la Venta en Core. Guid.Empty si fue rechazada.</summary>
    public int VentaIdCore { get; init; }

    /// <summary>Número de factura del Core (ej. "F-000042"). Vacío si fue rechazada.</summary>
    public string NumeroFactura { get; init; } = string.Empty;

    /// <summary>"Sincronizada" | "RechazadaCore"</summary>
    public string Resultado { get; init; } = string.Empty;

    public string? MotivoRechazo { get; init; }
    public DateTimeOffset SincronizadaEn { get; init; }
}

/// <summary>
/// P3 → P1: Informa que todas las transacciones de un lote offline fueron procesadas.
/// Permite al POS mostrar un resumen de reconciliación al cajero.
/// </summary>
public record ReconciliacionCompletadaEvent : IEvent
{
    public int SucursalId { get; init; }
    public int TotalTransacciones { get; init; }
    public int Aplicadas { get; init; }
    public int Rechazadas { get; init; }
    public DateTimeOffset CompletadaEn { get; init; }
}
