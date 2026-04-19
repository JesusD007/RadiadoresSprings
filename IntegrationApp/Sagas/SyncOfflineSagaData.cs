using NServiceBus;

namespace IntegrationApp.Sagas;

public class SyncOfflineSagaData : ContainSagaData
{
    public Guid IdTransaccionLocal { get; set; }
    public string SucursalId { get; set; } = string.Empty;
    public int Intentos { get; set; } = 0;
    public string LineasJson { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public Guid ClienteId { get; set; }
    public string CajeroId { get; set; } = string.Empty;
    public decimal MontoRecibido { get; set; }
    public DateTimeOffset FechaLocal { get; set; }
}
