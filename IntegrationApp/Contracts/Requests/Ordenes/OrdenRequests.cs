namespace IntegrationApp.Contracts.Requests.Ordenes;

public record CrearOrdenRequest(
    int ClienteId, string MetodoPago, string? DireccionEnvio,
    List<LineaOrdenRequest> Lineas, string? Notas = null);

public record LineaOrdenRequest(int ProductoId, int Cantidad);

