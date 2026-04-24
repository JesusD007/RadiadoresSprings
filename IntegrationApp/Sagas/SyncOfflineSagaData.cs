using NServiceBus;

namespace IntegrationApp.Sagas;

public class SyncOfflineSagaData : ContainSagaData
{
    public Guid IdTransaccionLocal { get; set; }
    public int SucursalId { get; set; }
    public int Intentos { get; set; } = 0;
    public string LineasJson { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public int CajeroId { get; set; }
    public decimal MontoRecibido { get; set; }
    public DateTimeOffset FechaLocal { get; set; }
}
