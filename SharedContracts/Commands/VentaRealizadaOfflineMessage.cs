using NServiceBus;

namespace SharedContracts.Commands;

/// <summary>
/// Caja POS (P1) → Integration (P3) vía RabbitMQ.
/// Se envía cuando la Caja completa una venta localmente mientras el Core
/// no está disponible (o directamente como canal principal de sincronización).
/// RabbitMQ garantiza durabilidad: si Integration está caído, el mensaje
/// se encola en el broker y se procesa cuando Integration vuelve.
/// </summary>
public record VentaRealizadaOfflineMessage : ICommand
{
    /// <summary>
    /// GUID único generado por la Caja en el momento de la venta.
    /// Se convierte en Idempotency-Key al enviar al Core.
    /// </summary>
    public Guid IdTransaccionLocal { get; init; }

    public string IdCajero { get; init; } = string.Empty;
    public string IdSucursal { get; init; } = string.Empty;
    public Guid ClienteId { get; init; }
    public string MetodoPago { get; init; } = string.Empty;
    public decimal MontoTotal { get; init; }
    public decimal MontoRecibido { get; init; }
    public IReadOnlyList<LineaVentaItem> Lineas { get; init; } = [];
    public DateTimeOffset FechaLocal { get; init; }
}

/// <summary>Línea de producto dentro de una VentaRealizadaOfflineMessage.</summary>
public record LineaVentaItem
{
    public int ProductoId { get; init; }
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
}
