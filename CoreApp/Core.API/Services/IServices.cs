using Core.API.DTOs.Requests;
using Core.API.DTOs.Responses;

namespace Core.API.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(string username, string password);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
    Task<bool> CambiarPasswordAsync(int usuarioId, string passwordActual, string passwordNuevo);
    Task<UsuarioResponse?> CrearUsuarioAsync(CrearUsuarioRequest req);
    Task<UsuarioResponse?> ActualizarUsuarioAsync(int id, ActualizarUsuarioRequest req);
    Task<IEnumerable<UsuarioResponse>> GetUsuariosAsync();
    Task<UsuarioResponse> RegistrarClienteWebAsync(RegistroWebRequest request);
    Task<IEnumerable<UsuarioMirrorResponse>> GetUsuariosMirrorAsync();
}

public interface IProductoService
{
    Task<PagedResult<ProductoResponse>> GetPagedAsync(int pagina, int tamano, string? busqueda, int? categoriaId);
    Task<ProductoResponse?> GetByIdAsync(int id);
    Task<ProductoResponse?> GetByCodigoAsync(string codigo);
    Task<ProductoResponse> CrearAsync(CrearProductoRequest req);
    Task<ProductoResponse?> ActualizarAsync(int id, ActualizarProductoRequest req);
    Task<bool> AjustarStockAsync(int id, int cantidad, string motivo);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<ProductoResponse>> GetStockBajoAsync();
    Task<int> ProcesarTransaccionOfflineAsync(TransaccionOfflineItem tx);
}

public interface ICategoriaService
{
    Task<IEnumerable<CategoriaResponse>> GetAllAsync();
    Task<CategoriaResponse?> GetByIdAsync(int id);
    Task<CategoriaResponse> CrearAsync(CrearCategoriaRequest req);
    Task<bool> ActualizarAsync(int id, CrearCategoriaRequest req);
    Task<bool> EliminarAsync(int id);
}

public interface IClienteService
{
    Task<PagedResult<ClienteResponse>> GetPagedAsync(int pagina, int tamano, string? busqueda);
    Task<ClienteResponse?> GetByIdAsync(int id);
    Task<ClienteResponse> CrearAsync(CrearClienteRequest req);
    Task<ClienteResponse?> ActualizarAsync(int id, ActualizarClienteRequest req);
    Task<bool> EliminarAsync(int id);
}

public interface IVentaService
{
    Task<PagedResult<VentaResponse>> GetPagedAsync(int pagina, int tamano, DateTime? desde, DateTime? hasta);
    Task<VentaResponse?> GetByIdAsync(int id);
    Task<VentaResponse?> GetByFacturaAsync(string numeroFactura);
    Task<VentaResponse> CrearAsync(CrearVentaRequest req, int usuarioId);
    Task<bool> CancelarAsync(int id, string motivo);
    Task<int> ProcesarTransaccionOfflineAsync(TransaccionOfflineItem tx);
}

public interface ICajaService
{
    Task<IEnumerable<CajaResponse>> GetCajasAsync(int? sucursalId);
    Task<SesionCajaResponse?> GetSesionActivaAsync(int cajaId);
    Task<SesionCajaResponse> AbrirSesionAsync(AbrirSesionCajaRequest req, int usuarioId);
    Task<SesionCajaResponse?> CerrarSesionAsync(CerrarSesionCajaRequest req);
    Task<IEnumerable<SesionCajaResponse>> GetHistorialAsync(int cajaId, int dias);
}

public interface IOrdenService
{
    Task<PagedResult<OrdenResponse>> GetPagedAsync(int pagina, int tamano, string? estado);
    Task<OrdenResponse?> GetByIdAsync(int id);
    Task<OrdenResponse> CrearAsync(CrearOrdenRequest req);
    Task<OrdenResponse?> CambiarEstadoAsync(int id, CambiarEstadoOrdenRequest req);
    Task<IEnumerable<OrdenResponse>> GetByClienteAsync(int clienteId);
}

public interface IPagoService
{
    Task<PagedResult<PagoResponse>> GetPagedAsync(int pagina, int tamano, int? clienteId);
    Task<PagoResponse?> GetByIdAsync(int id);
    Task<PagoResponse> RegistrarAsync(RegistrarPagoRequest req, int usuarioId);
}

public interface ICuentaCobrarService
{
    Task<PagedResult<CuentaCobrarResponse>> GetPagedAsync(int pagina, int tamano, string? estado, int? clienteId);
    Task<CuentaCobrarResponse?> GetByIdAsync(int id);
    Task<CuentaCobrarResponse> CrearAsync(CrearCuentaCobrarRequest req);
    Task<IEnumerable<CuentaCobrarResponse>> GetVencidasAsync();
}
