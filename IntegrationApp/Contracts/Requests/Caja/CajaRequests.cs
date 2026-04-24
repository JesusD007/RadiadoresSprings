namespace IntegrationApp.Contracts.Requests.Caja;

public record AbrirSesionCajaRequest(int CajaId, decimal MontoApertura);
public record CerrarSesionCajaRequest(int SesionId, decimal MontoCierre, string? Observaciones);
