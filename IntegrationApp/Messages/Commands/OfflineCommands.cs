using IntegrationApp.Contracts.Requests.Ventas;
using NServiceBus;

namespace IntegrationApp.Messages.Commands;

/// <summary>
/// La Caja POS (P1) envía este mensaje cuando IntegrationApp no está disponible.
/// P3 lo recibe, persiste la venta localmente y la encola para sincronización.
/// </summary>
public record VentaRealizadaOfflineMessage : ICommand
{
    public Guid IdTransaccionLocal { get; init; }  // Se convierte en Idempotency-Key
    public string IdCajero { get; init; } = string.Empty;
    public string IdSucursal { get; init; } = string.Empty;
    public Guid ClienteId { get; init; }
    public string MetodoPago { get; init; } = string.Empty;
    public decimal MontoTotal { get; init; }
    public decimal MontoRecibido { get; init; }
    public IReadOnlyList<LineaVentaDto> Lineas { get; init; } = [];
    public DateTimeOffset FechaLocal { get; init; }
}

/// <summary>
/// La Saga de P3 envía este comando al Core (P2) con las transacciones offline acumuladas.
/// </summary>
public record AplicarTransaccionesOfflineCommand : ICommand
{
    public Guid SagaId { get; init; }
    public IReadOnlyList<VentaOfflineDto> Transacciones { get; init; } = [];
}

public record VentaOfflineDto
{
    public Guid IdempotencyKey { get; init; }   // = IdTransaccionLocal
    public CrearVentaRequest Payload { get; init; } = null!;
    public DateTimeOffset FechaOriginal { get; init; }
}
