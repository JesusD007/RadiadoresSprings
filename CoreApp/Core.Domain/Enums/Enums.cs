namespace Core.Domain.Enums;

public enum RolUsuario
{
    Administrador = 1,
    Cajero        = 2,
    Vendedor      = 3,
    Almacenista   = 4,
    /// <summary>
    /// Cuenta de servicio para comunicación machine-to-machine.
    /// Usada por IntegrationApp (P3) para consumir los endpoints de Core.API vía HTTP + JWT.
    /// No tiene acceso a la consola ni a operaciones administrativas.
    /// </summary>
    ServicioWeb   = 5
}

public enum EstadoVenta
{
    Pendiente = 0,
    Completada = 1,
    Cancelada = 2,
    Reembolsada = 3
}

public enum MetodoPago
{
    Efectivo = 1,
    TarjetaCredito = 2,
    TarjetaDebito = 3,
    Transferencia = 4,
    PayPal = 5,
    Credito = 6
}

public enum EstadoOrden
{
    Recibida = 1,
    EnProceso = 2,
    Enviada = 3,
    Entregada = 4,
    Cancelada = 5,
    Devuelta = 6
}

public enum EstadoCuentaCobrar
{
    Pendiente = 1,
    PagoParcial = 2,
    Pagada = 3,
    Vencida = 4,
    Cancelada = 5
}

public enum EstadoSesionCaja
{
    Abierta = 1,
    Cerrada = 2,
    Cuadrada = 3
}

public enum TipoCliente
{
    Regular = 1,
    Mayorista = 2,
    VIP = 3,
    Anonimo = 4
}
