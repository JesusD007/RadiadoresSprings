namespace IntegrationApp.Contracts.Responses.Caja;

public record SesionCajaResponse(
    int Id, int CajaId, string NombreCaja, int UsuarioId, string NombreUsuario,
    DateTime FechaApertura, DateTime? FechaCierre,
    decimal MontoApertura, decimal? MontoCierre, decimal? MontoSistema, decimal? Diferencia,
    string Estado, int TotalVentas, decimal TotalVendido);
