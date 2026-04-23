namespace Core.API.DTOs.Responses;

// ── Genérico ──────────────────────────────────────────────────────────────────
public record ApiResponse<T>(bool Exito, string? Mensaje, T? Data);
public record PagedResult<T>(IEnumerable<T> Items, int Total, int Pagina, int TamañoPagina);

// ── Auth ──────────────────────────────────────────────────────────────────────
public record AuthResponse(string Token, string RefreshToken, DateTime Expiry, UsuarioResponse Usuario);

// ── Usuarios ──────────────────────────────────────────────────────────────────
public record UsuarioResponse(int Id, string Username, string Nombre, string Apellido,
    string Email, string Rol, int SucursalId, string NombreSucursal, bool EsActivo);

public record UsuarioMirrorResponse(int Id, string Username, string PasswordHash, string Rol,
    string Nombre, string? Apellido, string? Email, bool EsActivo);

// ── Productos ─────────────────────────────────────────────────────────────────
public record ProductoResponse(
    int Id, string Codigo, string Nombre, string? Descripcion,
    decimal Precio, decimal? PrecioOferta, decimal PrecioVigente,
    int Stock, int StockMinimo, bool StockBajo,
    int CategoriaId, string NombreCategoria, bool EsActivo,
    DateTime FechaCreacion);

// ── Sucursales ────────────────────────────────────────────────────────────────
public record SucursalResponse(int Id, string Nombre, string? Direccion, string? Telefono, bool EsActiva);

// ── Categorías ────────────────────────────────────────────────────────────────
public record CategoriaResponse(int Id, string Nombre, string? Descripcion, int TotalProductos, bool EsActiva);

// ── Clientes ──────────────────────────────────────────────────────────────────
public record ClienteResponse(
    int Id, string Nombre, string? Apellido, string NombreCompleto,
    string? Email, string? Telefono, string? Direccion, string? RFC,
    string Tipo, decimal LimiteCredito, decimal SaldoPendiente,
    decimal CreditoDisponible, bool EsActivo, DateTime FechaCreacion);

// ── Ventas ────────────────────────────────────────────────────────────────────
public record VentaResponse(
    int Id, string NumeroFactura, int SucursalId, int CajaId,
    int? ClienteId, string? NombreCliente, int UsuarioId, string NombreUsuario,
    DateTime Fecha, decimal Subtotal, decimal IVA, decimal Total, decimal Descuento,
    string MetodoPago, string Estado, bool EsOffline,
    List<LineaVentaResponse> Lineas);

public record LineaVentaResponse(
    int Id, int ProductoId, string CodigoProducto, string NombreProducto,
    int Cantidad, decimal PrecioUnitario, decimal Descuento, decimal Subtotal);

// ── Caja ──────────────────────────────────────────────────────────────────────
public record CajaResponse(int Id, int SucursalId, string NombreSucursal, string Numero, string Nombre, bool EsActiva);

public record SesionCajaResponse(
    int Id, int CajaId, string NombreCaja, int UsuarioId, string NombreUsuario,
    DateTime FechaApertura, DateTime? FechaCierre,
    decimal MontoApertura, decimal? MontoCierre, decimal? MontoSistema, decimal? Diferencia,
    string Estado, int TotalVentas, decimal TotalVendido);

// ── Órdenes ───────────────────────────────────────────────────────────────────
public record OrdenResponse(
    int Id, string NumeroOrden, int ClienteId, string NombreCliente,
    string Estado, DateTime Fecha, DateTime? FechaEntrega,
    decimal TotalOrden, string MetodoPago, string? DireccionEnvio,
    List<LineaOrdenResponse> Lineas);

public record LineaOrdenResponse(
    int Id, int ProductoId, string NombreProducto, int Cantidad,
    decimal PrecioUnitario, decimal Subtotal);

// ── Pagos ─────────────────────────────────────────────────────────────────────
public record PagoResponse(
    int Id, int ClienteId, string NombreCliente,
    int? VentaId, int? CuentaCobrarId,
    decimal Monto, string MetodoPago,
    DateTime Fecha, string? Referencia);

// ── Cuentas por Cobrar ────────────────────────────────────────────────────────
public record CuentaCobrarResponse(
    int Id, int ClienteId, string NombreCliente,
    int? VentaId, string NumeroFactura,
    decimal MontoOriginal, decimal MontoPagado, decimal SaldoPendiente,
    DateTime FechaEmision, DateTime FechaVencimiento,
    string Estado, bool EstaVencida);

// ── Health ────────────────────────────────────────────────────────────────────
public record HealthResponse(string Estado, string Version, DateTime Timestamp,
    string SqlServer, int TotalProductos, int TotalVentas);
