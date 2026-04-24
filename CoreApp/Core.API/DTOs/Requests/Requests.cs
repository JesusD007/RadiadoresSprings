namespace Core.API.DTOs.Requests;

// ── Auth ──────────────────────────────────────────────────────────────────────
public record LoginRequest(string Username, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record CambiarPasswordRequest(string PasswordActual, string PasswordNuevo);
public record RegistroWebRequest(string Username, string Password, string Nombre, string Apellido, string Email);

// ── Usuarios ──────────────────────────────────────────────────────────────────
public record CrearUsuarioRequest(
    string Username, string Password, string Nombre, string Apellido,
    string Email, string Rol, int SucursalId);

// ── Productos ─────────────────────────────────────────────────────────────────
public record CrearProductoRequest(
    string Codigo, string Nombre, string? Descripcion,
    decimal Precio, decimal? PrecioOferta, int Stock,
    int StockMinimo, int CategoriaId);

public record ActualizarProductoRequest(
    string Nombre, string? Descripcion,
    decimal Precio, decimal? PrecioOferta,
    int StockMinimo, int CategoriaId, bool EsActivo);

public record AjustarStockRequest(int Cantidad, string Motivo);

// ── Categorías ────────────────────────────────────────────────────────────────
public record CrearCategoriaRequest(string Nombre, string? Descripcion);

// ── Clientes ──────────────────────────────────────────────────────────────────
public record CrearClienteRequest(
    string Nombre, string? Apellido, string? Email,
    string? Telefono, string? Direccion, string? RFC,
    string Tipo, decimal LimiteCredito);

public record ActualizarClienteRequest(
    string Nombre, string? Apellido, string? Email,
    string? Telefono, string? Direccion, string? RFC,
    string Tipo, decimal LimiteCredito, bool EsActivo);

// ── Sincronización Offline (DTOs internos del Core) ──────────────────────────
/// <summary>
/// Representa una transacción de venta offline ya validada por Integration.
/// Usado internamente por VentaService para procesarla y persistirla en Core.
/// (Antes definido en CoreCommands.cs, movido aquí al eliminar el comando batch).
/// </summary>
public class TransaccionOfflineItem
{
    public string IdTransaccionLocal { get; set; } = string.Empty;
    public string CajeroId { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public int SucursalId { get; set; }
    public int CajaId { get; set; }
    public int SesionCajaId { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public decimal MontoTotal { get; set; }
    public decimal Descuento { get; set; }
    public string? Observaciones { get; set; }
    public List<LineaOfflineItem> Lineas { get; set; } = [];
    public DateTime FechaOffline { get; set; }
}

public class LineaOfflineItem
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

// ── Ventas ────────────────────────────────────────────────────────────────────
public record CrearVentaRequest(
    int SucursalId, int CajaId, int SesionCajaId,
    int? ClienteId, string MetodoPago,
    List<LineaVentaRequest> Lineas,
    decimal Descuento = 0,
    string? IdTransaccionLocal = null,
    string? Observaciones = null);

public record LineaVentaRequest(int ProductoId, int Cantidad, decimal? PrecioOverride = null);

// ── Caja ──────────────────────────────────────────────────────────────────────
public record AbrirSesionCajaRequest(int CajaId, decimal MontoApertura);
public record CerrarSesionCajaRequest(int SesionId, decimal MontoCierre, string? Observaciones);

// ── Órdenes ───────────────────────────────────────────────────────────────────
public record CrearOrdenRequest(
    int ClienteId, string MetodoPago, string? DireccionEnvio,
    List<LineaOrdenRequest> Lineas, string? Notas = null);

public record LineaOrdenRequest(int ProductoId, int Cantidad);
public record CambiarEstadoOrdenRequest(string NuevoEstado, string? Notas = null);

// ── Pagos ─────────────────────────────────────────────────────────────────────
public record RegistrarPagoRequest(
    int ClienteId, int? CuentaCobrarId, decimal Monto,
    string MetodoPago, string? Referencia, string? Notas);

// ── Cuentas por Cobrar ────────────────────────────────────────────────────────
public record CrearCuentaCobrarRequest(
    int ClienteId, int? VentaId, string NumeroFactura,
    decimal MontoOriginal, DateTime FechaVencimiento, string? Notas);
